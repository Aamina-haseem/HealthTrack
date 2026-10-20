using HealthTrack.Models;
namespace HealthTrack.ViewModels
{
    // One view model for the client's own dashboard; no other user's rows are loaded.
    public class ClientDashboardViewModel
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<WorkoutPlan> WorkoutPlans { get; init; } = Array.Empty<WorkoutPlan>();
        public IReadOnlyList<DietPlan> DietPlans { get; init; } = Array.Empty<DietPlan>();
        public IReadOnlyList<WorkoutLog> RecentWorkouts { get; init; } = Array.Empty<WorkoutLog>();
        public IReadOnlyList<MealLog> RecentMeals { get; init; } = Array.Empty<MealLog>();
        public HealthMeasurement? LatestMeasurement { get; init; }
        public IReadOnlyList<Appointment> UpcomingAppointments { get; init; } = Array.Empty<Appointment>();
        public IReadOnlyList<Feedback> RecentFeedback { get; init; } = Array.Empty<Feedback>();
        public int UnreadNotifications { get; init; }

        // ---- Chart data (built in ClientController from this client's own records) ----

        // Weight at every measurement, oldest first (line chart + sparkline)
        public IReadOnlyList<ChartPoint> WeightTrend { get; init; } = Array.Empty<ChartPoint>();
        // Latest weight minus the first recorded weight (kg); null until there are two measurements
        public double? WeightChange { get; init; }
        // WHO adult BMI band for the latest measurement, e.g. "Healthy weight"
        public string? BmiCategory { get; init; }

        // The last 7 days (oldest first) with totals per day
        public IReadOnlyList<string> WeekLabels { get; init; } = Array.Empty<string>();
        public IReadOnlyList<int> WeekMinutes { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> WeekBurned { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> WeekEaten { get; init; } = Array.Empty<int>();

        // Totals for this week and the week before (for the "vs last week" text)
        public int MinutesThisWeek { get; init; }
        public int MinutesLastWeek { get; init; }
        public int BurnedThisWeek { get; init; }
        public int BurnedLastWeek { get; init; }

        // Most logged activities in the last 30 days (sessions per exercise)
        public IReadOnlyList<ChartPoint> TopActivities { get; init; } = Array.Empty<ChartPoint>();
    }
}
