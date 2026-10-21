// =====================================================================
// FILE:    ViewModels/ProgressDashboardViewModel.cs
// PURPOSE: The data for the client's Progress page: all their measurements,
//          workouts and meals (for the charts and tables) plus summary numbers
//          for the last 30 days.
// USED BY: ProgressController.Index and Views/Progress/Index.cshtml (Task 7).
// =====================================================================

using HealthTrack.Models;   // HealthMeasurement, WorkoutLog, MealLog
namespace HealthTrack.ViewModels
{
    public class ProgressDashboardViewModel
    {
        public IReadOnlyList<HealthMeasurement> Measurements { get; init; } = Array.Empty<HealthMeasurement>();   // weight/BMI history
        public IReadOnlyList<WorkoutLog> Workouts { get; init; } = Array.Empty<WorkoutLog>();                     // workout history
        public IReadOnlyList<MealLog> Meals { get; init; } = Array.Empty<MealLog>();                              // meal history
        public double? WeightChangeKg { get; init; }                // latest minus first weight (null with fewer than 2 measurements)
        public int CaloriesBurnedLast30Days { get; init; }          // total burned in 30 days
        public int CaloriesConsumedLast30Days { get; init; }        // total eaten in 30 days
        public int WorkoutMinutesLast30Days { get; init; }          // total workout minutes in 30 days
    }
}
