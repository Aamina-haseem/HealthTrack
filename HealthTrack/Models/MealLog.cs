// =====================================================================
// FILE:    Models/MealLog.cs
// PURPOSE: A meal the Client actually ate, logged by the Client. Calories eaten
//          are compared with calories burned on the dashboard and reports.
// USED BY: LogsController (add/edit/delete), dashboards, ProgressController.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Range], [Display], [DataType]

namespace HealthTrack.Models
{
    /// <summary>
    /// A meal the Client actually ate (logged by the Client).
    /// </summary>
    public class MealLog
    {
        public int Id { get; set; }                                       // primary key

        [Required]                                                        // every log belongs to a client
        public string ClientId { get; set; } = string.Empty;              // foreign key to the client
        public ApplicationUser? Client { get; set; }                      // navigation property

        [DataType(DataType.Date)]                                         // date only, no time
        public DateTime Date { get; set; } = DateTime.Today;              // defaults to today

        [Display(Name = "Meal")]                                          // label "Meal" on forms
        public MealType MealType { get; set; }                            // Breakfast, Lunch, Dinner or Snack

        [Required, StringLength(250), Display(Name = "Food Items")]       // required, max 250 characters
        public string FoodItems { get; set; } = string.Empty;             // what was eaten

        [Range(0, 5000)]                                                  // calories in this meal
        public int Calories { get; set; }
    }
}