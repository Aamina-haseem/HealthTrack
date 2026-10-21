// =====================================================================
// FILE:    Models/WorkoutPlan.cs
// PURPOSE: A personalised workout plan a Trainer writes for one Client: title,
//          goal, difficulty, start/end dates and a list of exercises
//          (WorkoutPlanItem). One plan has many exercises (one-to-many).
// USED BY: WorkoutPlansController (trainer), ClientController (client's view),
//          LogsController (link a workout to a plan), dashboards.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Display], [DataType]

namespace HealthTrack.Models
{
    /// <summary>
    /// A personalised workout plan created by a Trainer for one Client.
    /// Contains a list of exercises (WorkoutPlanItem).
    /// </summary>
    public class WorkoutPlan
    {
        public int Id { get; set; }                                   // primary key

        [Required, StringLength(100)]                                 // required, max 100 characters
        public string Title { get; set; } = string.Empty;             // e.g. "8-Week Strength Builder"

        [StringLength(500)]                                           // optional, max 500 characters
        public string? Description { get; set; }

        [StringLength(100)]                                           // optional, max 100 characters
        public string? Goal { get; set; }          // e.g. "Lose 5kg", "Build strength"

        public DifficultyLevel Difficulty { get; set; }               // Beginner, Intermediate or Advanced

        [DataType(DataType.Date), Display(Name = "Start Date")]       // date only, no time
        public DateTime StartDate { get; set; } = DateTime.Today;     // defaults to today

        [DataType(DataType.Date), Display(Name = "End Date")]         // date only, no time
        public DateTime? EndDate { get; set; }                        // optional (open-ended plan)

        [Required]                                                    // the client the plan is for
        public string ClientId { get; set; } = string.Empty;          // foreign key to the client
        public ApplicationUser? Client { get; set; }                  // navigation property

        [Required]                                                    // the trainer who wrote it
        public string TrainerId { get; set; } = string.Empty;         // foreign key to the trainer
        public ApplicationUser? Trainer { get; set; }                 // navigation property

        public DateTime CreatedAt { get; set; } = DateTime.Now;       // when the plan was created

        // One plan has many exercises
        public List<WorkoutPlanItem> Items { get; set; } = new();     // starts as an empty list
    }
}