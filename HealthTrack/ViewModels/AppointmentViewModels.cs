// =====================================================================
// FILE:    ViewModels/AppointmentViewModels.cs
// PURPOSE: The appointment rules and view models (Task 6):
//            - AppointmentRules: the business rules for appointment times
//            - AppointmentListViewModel: the Appointments list page
//            - AppointmentCreateViewModel: the client's "Book an appointment" form
//            - AppointmentRescheduleViewModel: the trainer's "Update appointment" form
// KEY IDEAS: IValidatableObject (custom validation in Validate()), one shared
//            rule method so booking and rescheduling can never disagree.
// USED BY: AppointmentsController and the Views/Appointments pages.
// =====================================================================

using System.ComponentModel.DataAnnotations;              // [Required], ValidationResult, IValidatableObject
using HealthTrack.Models;                                 // Appointment
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;   // [ValidateNever]
using Microsoft.AspNetCore.Mvc.Rendering;                 // SelectListItem

namespace HealthTrack.ViewModels
{
    /// <summary>
    /// Shared business rules for appointment times, used when booking and rescheduling.
    /// </summary>
    public static class AppointmentRules
    {
        public const int OpeningHour = 6;     // earliest start 06:00
        public const int ClosingHour = 21;    // latest start 21:00
        public const int MaxDaysAhead = 90;   // can book up to 3 months ahead

        /// <summary>Returns an error message if the time breaks a rule, otherwise null.</summary>
        public static string? Check(DateTime appointmentDate)
        {
            if (appointmentDate < DateTime.Now.AddHours(1))                                     // too soon (or in the past)
                return "Please choose a time at least 1 hour from now.";
            if (appointmentDate > DateTime.Today.AddDays(MaxDaysAhead))                         // too far in the future
                return $"Appointments can only be booked up to {MaxDaysAhead} days ahead.";
            if (appointmentDate.Hour < OpeningHour || appointmentDate.Hour > ClosingHour)        // outside opening hours
                return $"Appointments must start between {OpeningHour:00}:00 and {ClosingHour:00}:00.";
            return null;                                                                         // null = the time is fine
        }
    }

    /// <summary>Data for the Appointments list page (clients and trainers share it).</summary>
    public class AppointmentListViewModel
    {
        public bool IsTrainer { get; init; }                     // true = show the trainer's buttons (approve, ...)
        public string? StatusFilter { get; init; }               // the selected status tab (null = all)
        public int PendingCount { get; init; }                   // number on the "Pending" tab
        public IReadOnlyList<Appointment> Appointments { get; init; } = Array.Empty<Appointment>();   // the rows to show
    }

    /// <summary>The "Book an appointment" form filled in by a client.</summary>
    public class AppointmentCreateViewModel : IValidatableObject   // lets us add the time rules below
    {
        [Required(ErrorMessage = "Please choose a trainer or health professional.")]   // must pick a trainer
        [Display(Name = "Trainer / Health Professional")]                              // form label
        public string TrainerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a date and time.")]                    // must pick a time
        [DataType(DataType.DateTime)]                                                  // date + time value
        [Display(Name = "Date & Time")]                                                // form label
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please tell the trainer what the appointment is about.")]                           // cannot be empty
        [StringLength(250, MinimumLength = 5, ErrorMessage = "The reason must be between 5 and 250 characters.")]     // 5 to 250 characters
        public string Reason { get; set; } = string.Empty;

        // Drop-down options; filled by the controller, never posted back
        [ValidateNever]
        public IEnumerable<SelectListItem> Trainers { get; set; } = Enumerable.Empty<SelectListItem>();

        // Called automatically by MVC model validation after the attribute checks
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var error = AppointmentRules.Check(AppointmentDate);                       // apply the shared time rules
            if (error != null)                                                         // a rule was broken
                yield return new ValidationResult(error, new[] { nameof(AppointmentDate) });   // show it under the date field
        }
    }

    /// <summary>The form a trainer uses to change the time of an appointment and add notes.</summary>
    public class AppointmentRescheduleViewModel : IValidatableObject   // lets us add the time rules below
    {
        public int Id { get; set; }                                    // the appointment being changed

        [ValidateNever]                                                // display only, not posted back
        public string ClientName { get; set; } = string.Empty;

        [ValidateNever]                                                // display only, not posted back
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a date and time.")]    // must pick a time
        [DataType(DataType.DateTime)]                                  // date + time value
        [Display(Name = "New Date & Time")]                            // form label
        public DateTime AppointmentDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot be longer than 500 characters.")]   // max 500 characters
        [Display(Name = "Trainer Notes (private, encrypted)")]                               // form label
        public string? TrainerNotes { get; set; }                                            // optional

        // Same time rules as booking
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var error = AppointmentRules.Check(AppointmentDate);                       // apply the shared time rules
            if (error != null)                                                         // a rule was broken
                yield return new ValidationResult(error, new[] { nameof(AppointmentDate) });   // show it under the date field
        }
    }
}
