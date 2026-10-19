using System.ComponentModel.DataAnnotations;
namespace HealthTrack.ViewModels
{
    public class HealthMeasurementCreateViewModel : IValidatableObject
    {
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;
        [Range(20, 300), Display(Name = "Weight (kg)")]
        public double WeightKg { get; set; }
        [Range(50, 250), Display(Name = "Height (cm)")]
        public double HeightCm { get; set; }
        [Range(30, 220), Display(Name = "Resting Heart Rate (bpm)")]
        public int? RestingHeartRate { get; set; }
        // Do not accept the sensitive Notes field until encrypted storage is ready.
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (Date.Date > DateTime.Today)
                yield return new ValidationResult("The measurement date cannot be in the future.", new[] { nameof(Date) });
        }
    }
}
