using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// One meal inside a DietPlan, e.g. "Breakfast – Oats with banana – 350 kcal – 07:30".
    /// </summary>
    public class DietPlanItem
    {
        public int Id { get; set; }

        public int DietPlanId { get; set; }
        public DietPlan? DietPlan { get; set; }

        [Display(Name = "Meal")]
        public MealType MealType { get; set; }

        [Required, StringLength(250), Display(Name = "Food Items")]
        public string FoodItems { get; set; } = string.Empty;

        [Range(0, 3000)]
        public int Calories { get; set; }

        // Used by Notifications for meal reminders
        [DataType(DataType.Time), Display(Name = "Meal Time")]
        public TimeSpan? MealTime { get; set; }
    }
}