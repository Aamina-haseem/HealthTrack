// =====================================================================
// FILE:    Areas/Identity/Pages/Account/Register.cshtml.cs
// PURPOSE: The code behind the Register page (Task 1). It started as Microsoft's
//          scaffolded Identity page and was customised for HealthTrack:
//            - extra profile fields (first/last name, date of birth, gender)
//            - a business rule: users must be 13 to 100 years old
//            - every self-registered user automatically gets the Client role
//              (trainers are created by the admin instead)
//            - try/catch so a failure shows a friendly message, not a crash
// KEY IDEAS: Razor Pages (OnGet / OnPostAsync), [BindProperty], UserManager hashes
//            the password, the password policy is set in Program.cs.
// =====================================================================

// Customised for HealthTrack: extra profile fields, age validation,
// automatic "Client" role and exception handling.
#nullable disable

using System.ComponentModel.DataAnnotations;   // [Required], [EmailAddress], [Compare], ...
using HealthTrack.Models;                      // ApplicationUser, AppRoles
using Microsoft.AspNetCore.Identity;           // UserManager, SignInManager, IdentityResult
using Microsoft.AspNetCore.Mvc;                // IActionResult, [BindProperty]
using Microsoft.AspNetCore.Mvc.RazorPages;     // PageModel

namespace HealthTrack.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Handles self-registration. Every self-registered user becomes a Client;
    /// Trainer accounts are created by the Admin (Task 4).
    /// </summary>
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;   // signs the new user in
        private readonly UserManager<ApplicationUser> _userManager;       // creates users and adds roles
        private readonly ILogger<RegisterModel> _logger;                  // writes to the log

        // Constructor: the Identity services are injected by ASP.NET Core
        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]                                 // filled automatically from the posted form
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }          // where to go after registering

        /// <summary>
        /// The registration form fields with their validation rules.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your first name.")]                                         // cannot be empty
            [StringLength(50)]                                                                                 // max 50 characters
            [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters.")]     // letters, spaces, ' and - only
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Please enter your last name.")]
            [StringLength(50)]
            [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters.")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Please enter your date of birth.")]
            [DataType(DataType.Date)]                                                                          // date picker
            [Display(Name = "Date of Birth")]
            public DateTime? DateOfBirth { get; set; }

            [Required(ErrorMessage = "Please select your gender.")]
            public string Gender { get; set; }

            [Required(ErrorMessage = "Please enter your email address.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]                               // must look like an email
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Please enter a password.")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
            [DataType(DataType.Password)]                                                                      // password box
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]       // must equal Password
            public string ConfirmPassword { get; set; }
        }

        // GET: shows the empty form
        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;                     // remember where to go afterwards
        }

        // POST: validates the form and creates the account
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");           // default: the home page

            // Business rule: users must be between 13 and 100 years old
            if (Input.DateOfBirth.HasValue)
            {
                int age = CalculateAge(Input.DateOfBirth.Value);
                if (age < 13 || age > 100)
                {
                    ModelState.AddModelError("Input.DateOfBirth", "You must be between 13 and 100 years old to register.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();   // show the form again with error messages
            }

            try
            {
                // Build the new account from the form
                var user = new ApplicationUser
                {
                    UserName = Input.Email.Trim(),     // signs in with the email
                    Email = Input.Email.Trim(),
                    FirstName = Input.FirstName.Trim(),
                    LastName = Input.LastName.Trim(),
                    DateOfBirth = Input.DateOfBirth,
                    Gender = Input.Gender,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                // Identity hashes the password before saving it - it is never stored as plain text
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // Every self-registered user is a Client
                    var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Client);
                    if (!roleResult.Succeeded)
                    {
                        // Undo the account so we never have a user without a role
                        await _userManager.DeleteAsync(user);
                        AddErrors(roleResult);
                        return Page();
                    }

                    _logger.LogInformation("New client registered: {Email}", user.Email);

                    await _signInManager.SignInAsync(user, isPersistent: false);   // sign them in straight away
                    TempData["Success"] = $"Welcome to HealthTrack, {user.FirstName}! Your account has been created.";
                    return LocalRedirect(returnUrl);   // LocalRedirect: only addresses inside the site (no open redirects)
                }

                // e.g. "Email is already taken", "Password needs an uppercase letter"
                AddErrors(result);
            }
            catch (Exception ex)                       // anything unexpected
            {
                _logger.LogError(ex, "Registration failed for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Something went wrong while creating your account. Please try again.");
            }

            return Page();                             // show the form again with the errors
        }

        /// <summary>Calculates age in full years from a date of birth.</summary>
        private static int CalculateAge(DateTime dateOfBirth)
        {
            int age = DateTime.Today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;   // birthday not reached yet this year
            return age;
        }

        /// <summary>Copies Identity errors into the form so the user can see them.</summary>
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}