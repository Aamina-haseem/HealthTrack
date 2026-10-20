using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

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
            if (appointmentDate < DateTime.Now.AddHours(1))
                return "Please choose a time at least 1 hour from now.";
            if (appointmentDate > DateTime.Today.AddDays(MaxDaysAhead))
                return $"Appointments can only be booked up to {MaxDaysAhead} days ahead.";
            if (appointmentDate.Hour < OpeningHour || appointmentDate.Hour > ClosingHour)
                return $"Appointments must start between {OpeningHour:00}:00 and {ClosingHour:00}:00.";
            return null;
        }
    }

    /// <summary>Data for the Appointments list page (clients and trainers share it).</summary>
    public class AppointmentListViewModel
    {
        public bool IsTrainer { get; init; }
        public string? StatusFilter { get; init; }
        public int PendingCount { get; init; }
        public IReadOnlyList<Appointment> Appointments { get; init; } = Array.Empty<Appointment>();
    }

    /// <summary>The "Book an appointment" form filled in by a client.</summary>
    public class AppointmentCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Please choose a trainer or health professional.")]
        [Display(Name = "Trainer / Health Professional")]
        public string TrainerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a date and time.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date & Time")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please tell the trainer what the appointment is about.")]
        [StringLength(250, MinimumLength = 5, ErrorMessage = "The reason must be between 5 and 250 characters.")]
        public string Reason { get; set; } = string.Empty;

        // Drop-down options; filled by the controller, never posted back
        [ValidateNever]
        public IEnumerable<SelectListItem> Trainers { get; set; } = Enumerable.Empty<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var error = AppointmentRules.Check(AppointmentDate);
            if (error != null)
                yield return new ValidationResult(error, new[] { nameof(AppointmentDate) });
        }
    }

    /// <summary>The form a trainer uses to change the time of an appointment and add notes.</summary>
    public class AppointmentRescheduleViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [ValidateNever]
        public string ClientName { get; set; } = string.Empty;

        [ValidateNever]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a date and time.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "New Date & Time")]
        public DateTime AppointmentDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot be longer than 500 characters.")]
        [Display(Name = "Trainer Notes (private, encrypted)")]
        public string? TrainerNotes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var error = AppointmentRules.Check(AppointmentDate);
            if (error != null)
                yield return new ValidationResult(error, new[] { nameof(AppointmentDate) });
        }
    }
}
