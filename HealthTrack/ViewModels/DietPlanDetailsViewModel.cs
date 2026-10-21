// =====================================================================
// FILE:    ViewModels/DietPlanDetailsViewModel.cs
// PURPOSE: The data for one diet plan's details page: the plan itself, the
//          client's name and the plan's meals.
// USED BY: DietPlansController.Details and Views/DietPlans/Details.cshtml (Task 5).
// =====================================================================

using HealthTrack.Models;   // DietPlan, DietPlanItem

namespace HealthTrack.ViewModels
{
    public class DietPlanDetailsViewModel
    {
        public required DietPlan Plan { get; init; }                                                 // "required": must be set when created
        public string ClientName { get; init; } = string.Empty;                                      // who the plan is for
        public IReadOnlyList<DietPlanItem> Meals { get; init; } = Array.Empty<DietPlanItem>();       // the planned meals
    }
}
