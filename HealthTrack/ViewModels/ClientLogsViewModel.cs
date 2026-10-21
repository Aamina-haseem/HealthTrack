// =====================================================================
// FILE:    ViewModels/ClientLogsViewModel.cs
// PURPOSE: The data for the client's "Log Activity" page: their own workouts,
//          meals and health measurements, shown in three tables.
// USED BY: LogsController.Index and Views/Logs/Index.cshtml (Task 5).
// =====================================================================

using HealthTrack.Models;   // WorkoutLog, MealLog, HealthMeasurement
namespace HealthTrack.ViewModels
{
    public class ClientLogsViewModel
    {
        public IReadOnlyList<WorkoutLog> Workouts { get; init; } = Array.Empty<WorkoutLog>();                   // the client's workouts
        public IReadOnlyList<MealLog> Meals { get; init; } = Array.Empty<MealLog>();                            // the client's meals
        public IReadOnlyList<HealthMeasurement> Measurements { get; init; } = Array.Empty<HealthMeasurement>(); // the client's measurements
    }
}
