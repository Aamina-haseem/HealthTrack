using HealthTrack.Models;
namespace HealthTrack.ViewModels
{
    public class TrainerDashboardViewModel
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<AssignedClientSummary> Clients { get; init; } = Array.Empty<AssignedClientSummary>();
        public int WorkoutPlanCount { get; init; }
        public int DietPlanCount { get; init; }
        public IReadOnlyList<Appointment> UpcomingAppointments { get; init; } = Array.Empty<Appointment>();
    }

    public class AssignedClientSummary
    {
        public string ClientId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public double? LatestWeightKg { get; init; }
        public double? LatestBmi { get; init; }
        public int WorkoutCount { get; init; }
    }
}
