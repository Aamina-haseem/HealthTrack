using HealthTrack.Models;
namespace HealthTrack.ViewModels
{
    public class ProgressDashboardViewModel
    {
        public IReadOnlyList<HealthMeasurement> Measurements { get; init; } = Array.Empty<HealthMeasurement>();
        public IReadOnlyList<WorkoutLog> Workouts { get; init; } = Array.Empty<WorkoutLog>();
        public IReadOnlyList<MealLog> Meals { get; init; } = Array.Empty<MealLog>();
        public double? WeightChangeKg { get; init; }
        public int CaloriesBurnedLast30Days { get; init; }
        public int CaloriesConsumedLast30Days { get; init; }
        public int WorkoutMinutesLast30Days { get; init; }
    }
}
