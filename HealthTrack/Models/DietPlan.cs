// =====================================================================
// FILE:    Models/DietPlan.cs
// PURPOSE: A personalised diet plan a Trainer writes for one Client: a title,
//          a daily calorie target, start/end dates and a list of planned meals
//          (DietPlanItem). One plan has many meals (one-to-many relationship).
// USED BY: DietPlansController (trainer), ClientController (client's view),
//          dashboards and reports.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Range], [Display], [DataType]

namespace HealthTrack.Models
{
    /// <summary>
    /// A personalised diet plan created by a Trainer for one Client.
    /// Contains a list of meals (DietPlanItem).
    /// </summary>
    public class DietPlan
    {
        public int Id { get; set; }                                   // primary key

        [Required, StringLength(100)]                                 // required, max 100 characters
        public string Title { get; set; } = string.Empty;             // e.g. "Balanced Weight Loss"

        [StringLength(500)]                                           // optional, max 500 characters
        public string? Description { get; set; }

        [Range(800, 6000), Display(Name = "Daily Calorie Target")]    // a sensible daily range
        public int DailyCalorieTarget { get; set; }                   // kcal per day

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

        // One plan has many meals
        public List<DietPlanItem> Items { get; set; } = new();        // starts as an empty list
    }
}
