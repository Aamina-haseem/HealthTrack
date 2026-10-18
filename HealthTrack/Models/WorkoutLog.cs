using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// A workout the Client actually completed (logged by the Client).
    /// Used for progress tracking and calories-burned charts.
    /// </summary>
    public class WorkoutLog
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        // Optional: which plan this workout belongs to
        public int? WorkoutPlanId { get; set; }
        public WorkoutPlan? WorkoutPlan { get; set; }

        [Required, StringLength(100), Display(Name = "Exercise / Activity")]
        public string ExerciseName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Range(1, 600), Display(Name = "Duration (min)")]
        public int DurationMinutes { get; set; }

        [Range(0, 5000), Display(Name = "Calories Burned")]
        public int CaloriesBurned { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }
    }
}