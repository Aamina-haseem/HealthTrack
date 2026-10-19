using HealthTrack.Models;
namespace HealthTrack.ViewModels
{
    public class ClientLogsViewModel
    {
        public IReadOnlyList<WorkoutLog> Workouts { get; init; } = Array.Empty<WorkoutLog>();
        public IReadOnlyList<MealLog> Meals { get; init; } = Array.Empty<MealLog>();
        public IReadOnlyList<HealthMeasurement> Measurements { get; init; } = Array.Empty<HealthMeasurement>();
    }
}
