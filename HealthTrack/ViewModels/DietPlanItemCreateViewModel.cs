// =====================================================================
// FILE:    ViewModels/DietPlanItemCreateViewModel.cs
// PURPOSE: The form fields for adding or editing one planned meal in a diet plan,
//          with their validation rules. The plan id comes from the URL and is
//          checked on the server, so it is not a form field.
// USED BY: DietPlansController (AddMeal / EditMeal) and Views/DietPlans/_MealForm.cshtml (Task 5).
// =====================================================================

using HealthTrack.Models;                      // MealType
using System.ComponentModel.DataAnnotations;   // [Required], [Range], [Display], [DataType]

namespace HealthTrack.ViewModels
{
    public class DietPlanItemCreateViewModel
    {
        [Display(Name = "Meal")]                                         // form label
        public MealType MealType { get; set; }                           // Breakfast, Lunch, Dinner or Snack

        [Required, StringLength(250), Display(Name = "Food Items")]      // required, max 250 characters
        public string FoodItems { get; set; } = string.Empty;

        [Range(0, 3000)]                                                 // calories for one meal
        public int Calories { get; set; }

        [DataType(DataType.Time), Display(Name = "Meal Time")]           // time picker
        public TimeSpan? MealTime { get; set; }                          // optional
    }
}
