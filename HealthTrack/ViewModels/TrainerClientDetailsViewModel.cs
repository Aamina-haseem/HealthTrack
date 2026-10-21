// =====================================================================
// FILE:    ViewModels/TrainerClientDetailsViewModel.cs
// PURPOSE: The data for the page where a trainer looks at ONE of their assigned
//          clients: the client, their plans, recent logs, measurements and the
//          feedback given. The controller only builds it after checking the
//          client is actively assigned to the signed-in trainer.
// USED BY: TrainerController.ClientDetails and Views/Trainer/ClientDetails.cshtml (Task 3).
// =====================================================================

using HealthTrack.Models;   // ApplicationUser, WorkoutPlan, DietPlan, ...
namespace HealthTrack.ViewModels
{
    public class TrainerClientDetailsViewModel
    {
        public required ApplicationUser Client { get; init; }                                                                // the client being viewed
        public IReadOnlyList<WorkoutPlan> WorkoutPlans { get; init; } = Array.Empty<WorkoutPlan>();                          // their workout plans
        public IReadOnlyList<DietPlan> DietPlans { get; init; } = Array.Empty<DietPlan>();                                   // their diet plans
        public IReadOnlyList<WorkoutLog> RecentWorkouts { get; init; } = Array.Empty<WorkoutLog>();                          // latest workouts
        public IReadOnlyList<MealLog> RecentMeals { get; init; } = Array.Empty<MealLog>();                                   // latest meals
        public IReadOnlyList<HealthMeasurement> RecentMeasurements { get; init; } = Array.Empty<HealthMeasurement>();        // latest measurements
        public IReadOnlyList<Feedback> Feedback { get; init; } = Array.Empty<Feedback>();                                    // feedback given so far
    }
}
