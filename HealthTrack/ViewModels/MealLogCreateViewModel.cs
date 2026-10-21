// =====================================================================
// FILE:    ViewModels/MealLogCreateViewModel.cs
// PURPOSE: The form fields a client fills in to log or edit a meal (date, meal
//          type, foods, calories), with their validation rules. The client id is
//          taken from the signed-in user on the server, so it is not a form field.
// USED BY: LogsController (AddMeal / EditMeal) and Views/Logs/_MealForm.cshtml (Task 5).
// =====================================================================

using HealthTrack.Models;                      // MealType
using System.ComponentModel.DataAnnotations;   // [Required], [Range], ValidationResult, IValidatableObject
namespace HealthTrack.ViewModels
{
    public class MealLogCreateViewModel : IValidatableObject   // lets us add the date rule below
    {
        [DataType(DataType.Date)]                                       // date picker
        public DateTime Date { get; set; } = DateTime.Today;            // defaults to today
        [Display(Name = "Meal")]                                        // form label
        public MealType MealType { get; set; }                          // Breakfast, Lunch, Dinner or Snack
        [Required, StringLength(250), Display(Name = "Food Items")]     // required, max 250 characters
        public string FoodItems { get; set; } = string.Empty;
        [Range(0, 5000)]                                                // calories in the meal
        public int Calories { get; set; }
        // Extra rule: a meal cannot be logged for a future date
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (Date.Date > DateTime.Today)                                                                          // future date?
                yield return new ValidationResult("The meal date cannot be in the future.", new[] { nameof(Date) }); // show it under Date
        }
    }
}
