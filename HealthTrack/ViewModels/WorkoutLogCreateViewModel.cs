// =====================================================================
// FILE:    ViewModels/WorkoutLogCreateViewModel.cs
// PURPOSE: The form fields a client fills in to log or edit a workout (activity,
//          date, minutes, calories, notes and an optional plan), with their
//          validation rules. The client id comes from the signed-in user, and a
//          chosen plan is checked to really belong to that client.
// USED BY: LogsController (AddWorkout / EditWorkout) and Views/Logs/_WorkoutForm.cshtml (Task 5).
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [Range], [Display], ValidationResult
namespace HealthTrack.ViewModels
{
    public class WorkoutLogCreateViewModel : IValidatableObject   // lets us add the date rule below
    {
        [Required, StringLength(100), Display(Name = "Exercise / Activity")]   // required, max 100 characters
        public string ExerciseName { get; set; } = string.Empty;
        [DataType(DataType.Date)]                                              // date picker
        public DateTime Date { get; set; } = DateTime.Today;                   // defaults to today
        [Range(1, 600), Display(Name = "Duration (min)")]                      // 1 minute to 10 hours
        public int DurationMinutes { get; set; }
        [Range(0, 5000), Display(Name = "Calories Burned")]                    // kcal burned
        public int CaloriesBurned { get; set; }
        [StringLength(250)]                                                    // optional, max 250 characters
        public string? Notes { get; set; }
        [Display(Name = "Workout Plan (optional)")]                            // form label
        public int? WorkoutPlanId { get; set; }                                // null = "No specific plan"
        // Extra rule: a workout cannot be logged for a future date
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (Date.Date > DateTime.Today)                                                                             // future date?
                yield return new ValidationResult("The workout date cannot be in the future.", new[] { nameof(Date) }); // show it under Date
        }
    }
}
