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
    }
}
