using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    // TrainerId comes from Identity; the selected client is checked on the server.
    public class DietPlanCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Select a client.")]
        public string ClientId { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(800, 6000), Display(Name = "Daily Calorie Target")]
        public int DailyCalorieTarget { get; set; } = 2000;

        [DataType(DataType.Date), Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date), Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)
                yield return new ValidationResult("End date must follow start date.", new[] { nameof(EndDate) });
        }
    }
}
