using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    // Holds the exercise fields submitted by the trainer.
    // The plan ID will come from the URL and be checked in the controller.
    public class WorkoutPlanItemCreateViewModel : IValidatableObject
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Exercise")]
        public string ExerciseName { get; set; } = string.Empty;

        [Display(Name = "Day")]
        public DayOfWeek DayOfWeek { get; set; }

        [Range(0, 20)]
        public int Sets { get; set; }

        [Range(0, 100)]
        public int Reps { get; set; }

        [Range(0, 300)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        // An exercise needs either sets and reps, or a duration.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (Sets == 0 && Reps == 0 && DurationMinutes == 0)
            {
                yield return new ValidationResult(
                    "Enter sets and reps, or a duration.",
                    new[] { nameof(Sets) });
            }

            if ((Sets == 0) != (Reps == 0))
            {
                yield return new ValidationResult(
                    "Enter both sets and reps.",
                    new[] { nameof(Sets), nameof(Reps) });
            }
        }
    }
}