using HealthTrack.Models;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class DietPlanItemCreateViewModel
    {
        [Display(Name = "Meal")]
        public MealType MealType { get; set; }

        [Required, StringLength(250), Display(Name = "Food Items")]
        public string FoodItems { get; set; } = string.Empty;

        [Range(0, 3000)]
        public int Calories { get; set; }

        [DataType(DataType.Time), Display(Name = "Meal Time")]
        public TimeSpan? MealTime { get; set; }
    }
}
