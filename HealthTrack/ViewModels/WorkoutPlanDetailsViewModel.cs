using HealthTrack.Models;

namespace HealthTrack.ViewModels
{
    // Supplies the plan and its exercises to the Details Razor view.
    public class WorkoutPlanDetailsViewModel
    {
        public required WorkoutPlan Plan { get; init; }

        public IReadOnlyList<WorkoutPlanItem> Exercises { get; init; }
            = Array.Empty<WorkoutPlanItem>();

        public string ClientName { get; init; } = string.Empty;
    }
}