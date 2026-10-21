// =====================================================================
// FILE:    ViewModels/HealthMeasurementCreateViewModel.cs
// PURPOSE: The form fields a client fills in to record or edit a health
//          measurement (date, weight, height, resting heart rate), with their
//          validation rules. BMI is not a field: it is calculated from weight and height.
// USED BY: LogsController (AddMeasurement / EditMeasurement) and
//          Views/Logs/_MeasurementForm.cshtml (Task 5).
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Range], [Display], [DataType], ValidationResult
namespace HealthTrack.ViewModels
{
    public class HealthMeasurementCreateViewModel : IValidatableObject   // lets us add the date rule below
    {
        [DataType(DataType.Date)]                                        // date picker
        public DateTime Date { get; set; } = DateTime.Today;             // defaults to today
        [Range(20, 300), Display(Name = "Weight (kg)")]                  // realistic range
        public double WeightKg { get; set; }
        [Range(50, 250), Display(Name = "Height (cm)")]                  // realistic range
        public double HeightCm { get; set; }
        [Range(30, 220), Display(Name = "Resting Heart Rate (bpm)")]     // optional, beats per minute
        public int? RestingHeartRate { get; set; }
        // The Notes field is not on this form; its database column is already encrypted if it is added later.
        // Extra rule: a measurement cannot be dated in the future
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (Date.Date > DateTime.Today)                                                                                  // future date?
                yield return new ValidationResult("The measurement date cannot be in the future.", new[] { nameof(Date) });  // show it under Date
        }
    }
}
