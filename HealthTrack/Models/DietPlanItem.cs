// =====================================================================
// FILE:    Models/DietPlanItem.cs
// PURPOSE: One planned meal inside a DietPlan (meal type, foods, calories and an
//          optional time). It is the "many" side of the DietPlan -> meals relationship.
// USED BY: DietPlansController (AddMeal / EditMeal), diet plan pages.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Range], [Display], [DataType]

namespace HealthTrack.Models
{
    /// <summary>
    /// One meal inside a DietPlan, e.g. "Breakfast – Oats with banana – 350 kcal – 07:30".
    /// </summary>
    public class DietPlanItem
    {
        public int Id { get; set; }                                         // primary key

        public int DietPlanId { get; set; }                                 // foreign key to the parent plan
        public DietPlan? DietPlan { get; set; }                             // navigation property back to the plan

        [Display(Name = "Meal")]                                            // label "Meal" on forms
        public MealType MealType { get; set; }                              // Breakfast, Lunch, Dinner or Snack

        [Required, StringLength(250), Display(Name = "Food Items")]         // required, max 250 characters
        public string FoodItems { get; set; } = string.Empty;               // e.g. "Oats, banana, milk"

        [Range(0, 3000)]                                                    // calories for one meal
        public int Calories { get; set; }

        // Used by Notifications for meal reminders
        [DataType(DataType.Time), Display(Name = "Meal Time")]              // shown as a time picker
        public TimeSpan? MealTime { get; set; }                             // optional, e.g. 07:30
    }
}