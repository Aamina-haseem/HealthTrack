// =====================================================================
// FILE:    ViewModels/WorkoutPlanDetailsViewModel.cs
// PURPOSE: The data for one workout plan's details page: the plan, its
//          exercises and the client's name.
// USED BY: WorkoutPlansController.Details and Views/WorkoutPlans/Details.cshtml (Task 5).
// =====================================================================

using HealthTrack.Models;   // WorkoutPlan, WorkoutPlanItem

namespace HealthTrack.ViewModels
{
    // Supplies the plan and its exercises to the Details Razor view.
    public class WorkoutPlanDetailsViewModel
    {
        public required WorkoutPlan Plan { get; init; }                 // "required": must be set when created

        public IReadOnlyList<WorkoutPlanItem> Exercises { get; init; }  // the plan's exercises
            = Array.Empty<WorkoutPlanItem>();                           // empty list by default

        public string ClientName { get; init; } = string.Empty;         // who the plan is for
    }
}