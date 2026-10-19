using HealthTrack.Models;

namespace HealthTrack.ViewModels
{
    public class DietPlanDetailsViewModel
    {
        public required DietPlan Plan { get; init; }
        public string ClientName { get; init; } = string.Empty;
        public IReadOnlyList<DietPlanItem> Meals { get; init; } = Array.Empty<DietPlanItem>();
    }
}
