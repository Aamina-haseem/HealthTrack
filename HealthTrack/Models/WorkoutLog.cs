// =====================================================================
// FILE:    Models/WorkoutLog.cs
// PURPOSE: A workout the Client actually did, logged by the Client: activity
//          name, date, minutes, calories burned and notes. It can optionally be
//          linked to one of the client's workout plans. These logs feed the
//          progress charts, dashboards and trainer reports.
// USED BY: LogsController, ClientController, TrainerController, ProgressController.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Range], [Display], [DataType]

namespace HealthTrack.Models
{
    /// <summary>
    /// A workout the Client actually completed (logged by the Client).
    /// Used for progress tracking and calories-burned charts.
    /// </summary>
    public class WorkoutLog
    {
        public int Id { get; set; }                                          // primary key

        [Required]                                                           // every log belongs to a client
        public string ClientId { get; set; } = string.Empty;                 // foreign key to the client
        public ApplicationUser? Client { get; set; }                         // navigation property

        // Optional: which plan this workout belongs to
        public int? WorkoutPlanId { get; set; }                              // null = not part of a plan
        public WorkoutPlan? WorkoutPlan { get; set; }                        // navigation property

        [Required, StringLength(100), Display(Name = "Exercise / Activity")] // required, max 100 characters
        public string ExerciseName { get; set; } = string.Empty;             // e.g. "Running"

        [DataType(DataType.Date)]                                            // date only, no time
        public DateTime Date { get; set; } = DateTime.Today;                 // defaults to today

        [Range(1, 600), Display(Name = "Duration (min)")]                    // 1 minute to 10 hours
        public int DurationMinutes { get; set; }

        [Range(0, 5000), Display(Name = "Calories Burned")]                  // kcal burned
        public int CaloriesBurned { get; set; }

        [StringLength(250)]                                                  // optional, max 250 characters
        public string? Notes { get; set; }
    }
}