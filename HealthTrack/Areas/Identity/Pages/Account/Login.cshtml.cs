// =====================================================================
// FILE:    Areas/Identity/Pages/Account/Login.cshtml.cs
// PURPOSE: The code behind the Login page, used by all three roles. It started
//          as Microsoft's scaffolded Identity page; only the logic was changed
//          (security rules, logging, reminders, role-based redirect).
// SECURITY: deactivated accounts are blocked, lockout after 5 wrong passwords,
//           one generic error message, LocalRedirect (no open redirects),
//           anti-forgery token checked automatically on the POST.
// =====================================================================

// Customised for HealthTrack (Task 1 + Security):
//  - blocks accounts the Admin has deactivated
//  - locks an account for 5 minutes after 5 wrong passwords (brute-force protection)
//  - shows one generic error so attackers cannot discover which emails are registered
//  - logs every login attempt and handles unexpected errors
//  - sends each role straight to its own dashboard
// Only the logic changed; the design in Login.cshtml is untouched.
#nullable disable

using System.ComponentModel.DataAnnotations;   // [Required], [EmailAddress], [Display]
using HealthTrack.Models;                      // ApplicationUser, AppRoles
using HealthTrack.Services;                    // INotificationService (login reminders)
using Microsoft.AspNetCore.Authentication;     // HttpContext.SignOutAsync
using Microsoft.AspNetCore.Identity;           // SignInManager, UserManager, IdentityConstants
using Microsoft.AspNetCore.Mvc;                // IActionResult, [BindProperty], [TempData]
using Microsoft.AspNetCore.Mvc.RazorPages;     // PageModel

namespace HealthTrack.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Handles login for all three roles (Client, Trainer, Admin).
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;   // checks the password and creates the login cookie
        private readonly UserManager<ApplicationUser> _userManager;       // finds users and their roles
        private readonly INotificationService _notificationService;       // creates today's reminders
        private readonly ILogger<LoginModel> _logger;                     // writes to the log

        // Constructor: the services are injected by ASP.NET Core
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

        [BindProperty]                                 // filled automatically from the posted form
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }          // the page the user wanted before logging in

        [TempData]                                     // survives one redirect
        public string ErrorMessage { get; set; }

        /// <summary>The login form fields with their validation rules.</summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your email address.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]   // must look like an email
            public string Email { get; set; }

            [Required(ErrorMessage = "Please enter your password.")]
            [DataType(DataType.Password)]                                          // password box
            public string Password { get; set; }

            [Display(Name = "Remember me?")]                                       // keeps the cookie after the browser closes
            public bool RememberMe { get; set; }
        }

        // GET: shows the login form
        public async Task OnGetAsync(string returnUrl = null)
        {
            // Show an error passed from another page (e.g. an external login failure)
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");           // default: the home page

            // Clear any old external login cookie so every login starts clean
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        // POST: checks the email and password
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Empty or badly formatted fields: show the form again with the messages
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string email = Input.Email.Trim();                         // ignore spaces typed by mistake
                var user = await _userManager.FindByEmailAsync(email);     // null if no such account

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

                // Correct password: the login cookie has been created
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

                    // The admin's first name is only "System", so the admin is greeted by full name
                    // ("System Admin"), the same as on the Admin dashboard. Everyone else gets their first name.
                    bool isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
                    TempData["Success"] = $"Welcome back, {(isAdmin ? user.FullName : user.FirstName)}!";   // green message on the next page

                    // If the user was sent here from a protected page, go back to it;
                    // otherwise open the dashboard that matches their role.
                    if (returnUrl != Url.Content("~/"))
                    {
                        return LocalRedirect(returnUrl);   // LocalRedirect blocks redirects to other websites
                    }
                    return RedirectToAction("Index", await GetDashboardControllerAsync(user), new { area = "" });
                }

                // Two-factor login (part of the Identity template, not used by HealthTrack)
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                // Too many wrong passwords: show the "locked out" page
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
            catch (Exception ex)                       // anything unexpected (e.g. the database is down)
            {
                _logger.LogError(ex, "Login failed for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Something went wrong while logging in. Please try again.");
            }

            return Page();                             // show the form again with the error
        }

        /// <summary>Returns the controller of the dashboard for the user's role.</summary>
        private async Task<string> GetDashboardControllerAsync(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin)) return "Admin";       // AdminController.Index
            if (await _userManager.IsInRoleAsync(user, AppRoles.Trainer)) return "Trainer";   // TrainerController.Index
            return "Client";                                                                  // everyone else is a client
        }
    }
}
