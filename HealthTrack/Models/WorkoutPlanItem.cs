// =====================================================================
// FILE:    Models/WorkoutPlanItem.cs
// PURPOSE: One exercise inside a WorkoutPlan: name, day of the week, and either
//          sets x reps (counted exercise) or a duration (timed exercise).
//          It is the "many" side of the WorkoutPlan -> exercises relationship.
// USED BY: WorkoutPlansController (AddExercise / EditExercise), plan pages.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Range], [Display]

namespace HealthTrack.Models
{
    /// <summary>
    /// One exercise inside a WorkoutPlan, e.g. "Squats – Monday – 3 x 12".
    /// </summary>
    public class WorkoutPlanItem
    {
        public int Id { get; set; }                                      // primary key

        public int WorkoutPlanId { get; set; }                           // foreign key to the parent plan
        public WorkoutPlan? WorkoutPlan { get; set; }                    // navigation property back to the plan

        [Required, StringLength(100), Display(Name = "Exercise")]        // required, max 100 characters
        public string ExerciseName { get; set; } = string.Empty;         // e.g. "Squats"

        [Display(Name = "Day")]                                          // label "Day" on forms
        public DayOfWeek DayOfWeek { get; set; }                         // built-in .NET enum: Sunday ... Saturday

        [Range(0, 20)]                                                   // 0 = not a counted exercise
        public int Sets { get; set; }

        [Range(0, 100)]                                                  // repetitions per set
        public int Reps { get; set; }

        [Range(0, 300), Display(Name = "Duration (min)")]                // 0 = not a timed exercise
        public int DurationMinutes { get; set; }

        [StringLength(250)]                                              // optional, max 250 characters
        public string? Notes { get; set; }                               // e.g. "rest 60 seconds between sets"
    }
}