using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// A meal the Client actually ate (logged by the Client).
    /// </summary>
    public class MealLog
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Display(Name = "Meal")]
        public MealType MealType { get; set; }

        [Required, StringLength(250), Display(Name = "Food Items")]
        public string FoodItems { get; set; } = string.Empty;

        [Range(0, 5000)]
        public int Calories { get; set; }
    }
}