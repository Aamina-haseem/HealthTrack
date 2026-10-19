namespace HealthTrack.ViewModels
{
    public class TrainerReportViewModel
    {
        public string? FilteredClientName { get; init; }
        public IReadOnlyList<ClientPerformanceRow> Clients { get; init; } = Array.Empty<ClientPerformanceRow>();
        public IReadOnlyList<PopularActivityRow> PopularActivities { get; init; } = Array.Empty<PopularActivityRow>();
    }

    public class ClientPerformanceRow
    {
        public string ClientId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int WorkoutCount { get; init; }
        public int MealCount { get; init; }
        public int WorkoutMinutes { get; init; }
        public int CaloriesBurned { get; init; }
        public int WorkoutPlanCount { get; init; }
        public int DietPlanCount { get; init; }
        public double? LatestWeightKg { get; init; }
        public double? LatestBmi { get; init; }
        public DateTime? LastActivity { get; init; }
    }

    public class PopularActivityRow
    {
        public string Activity { get; init; } = string.Empty;
        public int Sessions { get; init; }
    }
}
