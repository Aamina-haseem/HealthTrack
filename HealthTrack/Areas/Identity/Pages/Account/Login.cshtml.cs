// Customised for HealthTrack (Task 1 + Security):
//  - blocks accounts the Admin has deactivated
//  - locks an account for 5 minutes after 5 wrong passwords (brute-force protection)
//  - shows one generic error so attackers cannot discover which emails are registered
//  - logs every login attempt and handles unexpected errors
//  - sends each role straight to its own dashboard
// Only the logic changed; the design in Login.cshtml is untouched.
#nullable disable

using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HealthTrack.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Handles login for all three roles (Client, Trainer, Admin).
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>The login form fields with their validation rules.</summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your email address.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Please enter your password.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear any old external login cookie so every login starts clean
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string email = Input.Email.Trim();
                var user = await _userManager.FindByEmailAsync(email);

                // 1. Deactivated accounts cannot log in (set by the Admin on the Users page)
                if (user != null && !user.IsActive)
                {
                    _logger.LogWarning("Login blocked for deactivated account {Email}", email);
                    ModelState.AddModelError(string.Empty,
                        "Your account has been deactivated. Please contact the administrator.");
                    return Page();
                }

                // 2. Check the password. lockoutOnFailure: true -> after 5 wrong attempts
                //    the account is locked for 5 minutes (settings in Program.cs)
                var result = await _signInManager.PasswordSignInAsync(
                    email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded && user != null)
                {
                    _logger.LogInformation("User {Email} logged in.", email);

                    // Create today's reminders (appointments, workouts, meals) straight away.
                    // A reminder problem must never stop the user from logging in.
                    try
                    {
                        await _notificationService.GenerateRemindersAsync(user.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Could not create reminders for {Email}", email);
                    }

                    TempData["Success"] = $"Welcome back, {user.FirstName}!";

                    // If the user was sent here from a protected page, go back to it;
                    // otherwise open the dashboard that matches their role.
                    if (returnUrl != Url.Content("~/"))
                    {
                        return LocalRedirect(returnUrl);   // LocalRedirect blocks redirects to other websites
                    }
                    return RedirectToAction("Index", await GetDashboardControllerAsync(user), new { area = "" });
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Account {Email} locked out after repeated failed attempts.", email);
                    return RedirectToPage("./Lockout");
                }

                // 3. Same message for a wrong email OR a wrong password,
                //    so attackers cannot find out which accounts exist
                _logger.LogWarning("Failed login attempt for {Email}", email);
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Something went wrong while logging in. Please try again.");
            }

            return Page();
        }

        /// <summary>Returns the controller of the dashboard for the user's role.</summary>
        private async Task<string> GetDashboardControllerAsync(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin)) return "Admin";
            if (await _userManager.IsInRoleAsync(user, AppRoles.Trainer)) return "Trainer";
            return "Client";
        }
    }
}
