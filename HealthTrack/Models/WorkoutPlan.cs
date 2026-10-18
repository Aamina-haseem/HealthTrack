using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// A personalised workout plan created by a Trainer for one Client.
    /// Contains a list of exercises (WorkoutPlanItem).
    /// </summary>
    public class WorkoutPlan
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Goal { get; set; }          // e.g. "Lose 5kg", "Build strength"

        public DifficultyLevel Difficulty { get; set; }

        [DataType(DataType.Date), Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date), Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        [Required]
        public string TrainerId { get; set; } = string.Empty;
        public ApplicationUser? Trainer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // One plan has many exercises
        public List<WorkoutPlanItem> Items { get; set; } = new();
    }
}