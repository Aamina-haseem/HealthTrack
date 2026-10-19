using System.ComponentModel.DataAnnotations;
namespace HealthTrack.ViewModels
{
    public class WorkoutLogCreateViewModel : IValidatableObject
    {
        [Required, StringLength(100), Display(Name = "Exercise / Activity")]
        public string ExerciseName { get; set; } = string.Empty;
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;
        [Range(1, 600), Display(Name = "Duration (min)")]
        public int DurationMinutes { get; set; }
        [Range(0, 5000), Display(Name = "Calories Burned")]
        public int CaloriesBurned { get; set; }
        [StringLength(250)]
        public string? Notes { get; set; }
        [Display(Name = "Workout Plan (optional)")]
        public int? WorkoutPlanId { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (Date.Date > DateTime.Today)
                yield return new ValidationResult("The workout date cannot be in the future.", new[] { nameof(Date) });
        }
    }
}
