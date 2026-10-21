// =====================================================================
// FILE:    ViewModels/DietPlanCreateViewModel.cs
// PURPOSE: The form fields a trainer fills in to create or edit a diet plan,
//          with their validation rules. The trainer's own id is NOT a form field:
//          it is taken from the signed-in user on the server, so it cannot be faked.
// USED BY: DietPlansController (Create / Edit) and Views/DietPlans/_PlanForm.cshtml (Task 5).
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [Range], ValidationResult, IValidatableObject

namespace HealthTrack.ViewModels
{
    // TrainerId comes from Identity; the selected client is checked on the server.
    public class DietPlanCreateViewModel : IValidatableObject   // lets us add the date rule below
    {
        [Required(ErrorMessage = "Select a client.")]                  // must pick a client
        public string ClientId { get; set; } = string.Empty;

        [Required, StringLength(100)]                                  // required, max 100 characters
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]                                            // optional, max 500 characters
        public string? Description { get; set; }

        [Range(800, 6000), Display(Name = "Daily Calorie Target")]     // a sensible daily range
        public int DailyCalorieTarget { get; set; } = 2000;            // default shown on the form

        [DataType(DataType.Date), Display(Name = "Start Date")]        // date picker
        public DateTime StartDate { get; set; } = DateTime.Today;      // defaults to today

        [DataType(DataType.Date), Display(Name = "End Date")]          // date picker
        public DateTime? EndDate { get; set; }                         // optional

        // Extra rule checked after the attributes: the end date cannot be before the start date
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)                                            // end before start?
                yield return new ValidationResult("End date must follow start date.", new[] { nameof(EndDate) });   // show it under End Date
        }
    }
}
