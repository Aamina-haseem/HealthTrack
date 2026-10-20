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

        // ---- Chart data (only clients actively assigned to this trainer) ----

        // The last 14 days (oldest first) and the workout minutes all assigned clients logged each day
        public IReadOnlyList<string> DayLabels { get; init; } = Array.Empty<string>();
        public IReadOnlyList<int> DailyMinutes { get; init; } = Array.Empty<int>();

        // Workouts logged by assigned clients this week and the week before
        public int WorkoutsThisWeek { get; init; }
        public int WorkoutsLastWeek { get; init; }

        // Appointment requests still waiting for this trainer's approval
        public int PendingRequests { get; init; }

        // Workout minutes per client and most logged activities, both for the last 30 days
        public IReadOnlyList<ChartPoint> MinutesByClient { get; init; } = Array.Empty<ChartPoint>();
        public IReadOnlyList<ChartPoint> TopActivities { get; init; } = Array.Empty<ChartPoint>();
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
