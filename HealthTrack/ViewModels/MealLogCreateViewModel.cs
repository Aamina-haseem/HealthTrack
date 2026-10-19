using HealthTrack.Models;
using System.ComponentModel.DataAnnotations;
namespace HealthTrack.ViewModels
{
    public class MealLogCreateViewModel : IValidatableObject
    {
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;
        [Display(Name = "Meal")]
        public MealType MealType { get; set; }
        [Required, StringLength(250), Display(Name = "Food Items")]
        public string FoodItems { get; set; } = string.Empty;
        [Range(0, 5000)]
        public int Calories { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (Date.Date > DateTime.Today)
                yield return new ValidationResult("The meal date cannot be in the future.", new[] { nameof(Date) });
        }
    }
}
