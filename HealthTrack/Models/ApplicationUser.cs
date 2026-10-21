// =====================================================================
// FILE:    Models/ApplicationUser.cs
// PURPOSE: The user account for every person in HealthTrack (Client, Trainer or
//          Admin). It extends ASP.NET Core Identity's IdentityUser with profile
//          fields (name, date of birth, gender, trainer specialization) and an
//          IsActive flag the Admin uses to deactivate accounts.
// KEY IDEAS: inheritance (": IdentityUser"), data annotations for validation,
//            roles are stored separately by Identity in the AspNetUserRoles table.
// =====================================================================

using Microsoft.AspNetCore.Identity;               // IdentityUser (Email, PasswordHash, UserName, ...)
using System.ComponentModel.DataAnnotations;       // [Required], [StringLength], [Display], [DataType]

namespace HealthTrack.Models
{
    /// <summary>
    /// Represents every person who uses HealthTrack (Client, Trainer or Admin).
    /// Extends the built-in IdentityUser, which already provides Email, PasswordHash, etc.
    /// The person's role is stored separately by Identity (AspNetUserRoles table).
    /// </summary>
    public class ApplicationUser : IdentityUser   // inherits all the login fields from Identity
    {
        [Required, StringLength(50)]                   // must be filled in, max 50 characters
        [Display(Name = "First Name")]                 // label shown on forms
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]                   // must be filled in, max 50 characters
        [Display(Name = "Last Name")]                  // label shown on forms
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date)]                      // shown as a date picker (no time part)
        [Display(Name = "Date of Birth")]              // label shown on forms
        public DateTime? DateOfBirth { get; set; }     // nullable: trainers created by the admin may not have one

        [StringLength(10)]                             // e.g. "Female", "Male", "Other"
        public string? Gender { get; set; }            // optional

        // Only used for Trainers, e.g. "Fitness Trainer", "Nutritionist", "Physiotherapist"
        [StringLength(50)]                             // max 50 characters
        public string? Specialization { get; set; }    // null for clients and the admin


        // Used by the Admin dashboard to show user activity
        public DateTime CreatedAt { get; set; } = DateTime.Now;   // set automatically when the account is created

        // Lets the Admin deactivate an account without deleting its health data
        public bool IsActive { get; set; } = true;                // new accounts start active

        // Read-only helper used in views, e.g. "Welcome, John Silva"
        public string FullName => $"{FirstName} {LastName}";      // not stored in the database (calculated)
    }
}