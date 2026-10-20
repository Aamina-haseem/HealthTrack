// Customised for HealthTrack: extra profile fields, age validation,
// automatic "Client" role and exception handling.
#nullable disable

using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HealthTrack.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Handles self-registration. Every self-registered user becomes a Client;
    /// Trainer accounts are created by the Admin (Task 4).
    /// </summary>
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        /// <summary>
        /// The registration form fields with their validation rules.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your first name.")]
            [StringLength(50)]
            [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters.")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Please enter your last name.")]
            [StringLength(50)]
            [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters.")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Please enter your date of birth.")]
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime? DateOfBirth { get; set; }

            [Required(ErrorMessage = "Please select your gender.")]
            public string Gender { get; set; }

            [Required(ErrorMessage = "Please enter your email address.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Please enter a password.")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

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
                var user = new ApplicationUser
                {
                    UserName = Input.Email.Trim(),
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

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["Success"] = $"Welcome to HealthTrack, {user.FirstName}! Your account has been created.";
                    return LocalRedirect(returnUrl);
                }

                // e.g. "Email is already taken", "Password needs an uppercase letter"
                AddErrors(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Something went wrong while creating your account. Please try again.");
            }

            return Page();
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