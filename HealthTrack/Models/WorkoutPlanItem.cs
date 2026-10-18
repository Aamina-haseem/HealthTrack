using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// One exercise inside a WorkoutPlan, e.g. "Squats – Monday – 3 x 12".
    /// </summary>
    public class WorkoutPlanItem
    {
        public int Id { get; set; }

        public int WorkoutPlanId { get; set; }
        public WorkoutPlan? WorkoutPlan { get; set; }

        [Required, StringLength(100), Display(Name = "Exercise")]
        public string ExerciseName { get; set; } = string.Empty;

        [Display(Name = "Day")]
        public DayOfWeek DayOfWeek { get; set; }

        [Range(0, 20)]
        public int Sets { get; set; }

        [Range(0, 100)]
        public int Reps { get; set; }

        [Range(0, 300), Display(Name = "Duration (min)")]
        public int DurationMinutes { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }
    }
}