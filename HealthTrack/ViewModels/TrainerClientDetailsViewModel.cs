using HealthTrack.Models;
namespace HealthTrack.ViewModels
{
    public class TrainerClientDetailsViewModel
    {
        public required ApplicationUser Client { get; init; }
        public IReadOnlyList<WorkoutPlan> WorkoutPlans { get; init; } = Array.Empty<WorkoutPlan>();
        public IReadOnlyList<DietPlan> DietPlans { get; init; } = Array.Empty<DietPlan>();
        public IReadOnlyList<WorkoutLog> RecentWorkouts { get; init; } = Array.Empty<WorkoutLog>();
        public IReadOnlyList<MealLog> RecentMeals { get; init; } = Array.Empty<MealLog>();
        public IReadOnlyList<HealthMeasurement> RecentMeasurements { get; init; } = Array.Empty<HealthMeasurement>();
        public IReadOnlyList<Feedback> Feedback { get; init; } = Array.Empty<Feedback>();
    }
}
