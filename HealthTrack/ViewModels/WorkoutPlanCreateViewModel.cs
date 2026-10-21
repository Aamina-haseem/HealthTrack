// =====================================================================
// FILE:    ViewModels/WorkoutPlanCreateViewModel.cs
// PURPOSE: The form fields a trainer fills in to create or edit a workout plan,
//          with their validation rules. The trainer's id is NOT a form field: it
//          is taken from the signed-in user on the server, and the chosen client
//          is checked to be assigned to that trainer.
// USED BY: WorkoutPlansController (Create / Edit) and Views/WorkoutPlans/Create + Edit (Task 5).
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], ValidationResult, IValidatableObject
using HealthTrack.Models;                      // DifficultyLevel

namespace HealthTrack.ViewModels
{
    public class WorkoutPlanCreateViewModel : IValidatableObject   // lets us add the date rule below
    {
        [Required(ErrorMessage = "Select a client.")]                  // must pick a client
        public string ClientId { get; set; } = string.Empty;

        [Required, StringLength(100)]                                  // required, max 100 characters
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]                                            // optional, max 500 characters
        public string? Description { get; set; }

        [StringLength(100)]                                            // optional, max 100 characters
        public string? Goal { get; set; }

        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;   // default level

        [DataType(DataType.Date), Display(Name = "Start Date")]        // date picker
        public DateTime StartDate { get; set; } = DateTime.Today;      // defaults to today

        [DataType(DataType.Date), Display(Name = "End Date")]          // date picker
        public DateTime? EndDate { get; set; }                         // optional

        // Check a rule involving two fields after the form is submitted.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)   // end before start?
            {
                yield return new ValidationResult(
                    "End Date cannot be before Start Date.",               // the message shown
                    new[] { nameof(EndDate) });                            // under the End Date field
            }
        }
    }
}