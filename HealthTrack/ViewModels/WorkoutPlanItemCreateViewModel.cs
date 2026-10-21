// =====================================================================
// FILE:    ViewModels/WorkoutPlanItemCreateViewModel.cs
// PURPOSE: The form fields for adding or editing one exercise in a workout plan,
//          with their validation rules: an exercise needs either sets AND reps
//          (counted) or a duration (timed). The plan id comes from the URL and
//          is checked in the controller, so it is not a form field.
// USED BY: WorkoutPlansController (AddExercise / EditExercise) and their views (Task 5).
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [Range], ValidationResult, IValidatableObject

namespace HealthTrack.ViewModels
{
    // Holds the exercise fields submitted by the trainer.
    // The plan ID will come from the URL and be checked in the controller.
    public class WorkoutPlanItemCreateViewModel : IValidatableObject   // lets us add the rules below
    {
        [Required]                                    // cannot be empty
        [StringLength(100)]                           // max 100 characters
        [Display(Name = "Exercise")]                  // form label
        public string ExerciseName { get; set; } = string.Empty;

        [Display(Name = "Day")]                       // form label
        public DayOfWeek DayOfWeek { get; set; }      // Sunday ... Saturday

        [Range(0, 20)]                                // 0 = not a counted exercise
        public int Sets { get; set; }

        [Range(0, 100)]                               // repetitions per set
        public int Reps { get; set; }

        [Range(0, 300)]                               // 0 = not a timed exercise
        [Display(Name = "Duration (minutes)")]        // form label
        public int DurationMinutes { get; set; }

        [StringLength(250)]                           // optional, max 250 characters
        public string? Notes { get; set; }

        // An exercise needs either sets and reps, or a duration.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (Sets == 0 && Reps == 0 && DurationMinutes == 0)       // nothing entered at all
            {
                yield return new ValidationResult(
                    "Enter sets and reps, or a duration.",            // the message shown
                    new[] { nameof(Sets) });                          // under the Sets field
            }

            if ((Sets == 0) != (Reps == 0))                           // only one of sets/reps entered
            {
                yield return new ValidationResult(
                    "Enter both sets and reps.",                      // the message shown
                    new[] { nameof(Sets), nameof(Reps) });            // under both fields
            }
        }
    }
}