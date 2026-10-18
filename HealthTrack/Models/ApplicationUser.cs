using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// Represents every person who uses HealthTrack (Client, Trainer or Admin).
    /// Extends the built-in IdentityUser, which already provides Email, PasswordHash, etc.
    /// The person's role is stored separately by Identity (AspNetUserRoles table).
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }        // Only used for Trainers, e.g. "Fitness Trainer", "Nutritionist", "Physiotherapist"
        [StringLength(50)]
        public string? Specialization { get; set; }


        // Used by the Admin dashboard to show user activity
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Lets the Admin deactivate an account without deleting its health data
        public bool IsActive { get; set; } = true;

        // Read-only helper used in views, e.g. "Welcome, John Silva"
        public string FullName => $"{FirstName} {LastName}";
    }
}