using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;

namespace HealthTrack.ViewModels
{
    public class WorkoutPlanCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Select a client.")]
        public string ClientId { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Goal { get; set; }

        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

        [DataType(DataType.Date), Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date), Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        // Check a rule involving two fields after the form is submitted.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)
            {
                yield return new ValidationResult(
                    "End Date cannot be before Start Date.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}