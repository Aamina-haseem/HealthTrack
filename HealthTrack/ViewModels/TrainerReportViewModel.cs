// =====================================================================
// FILE:    ViewModels/TrainerReportViewModel.cs
// PURPOSE: The data for the trainer's performance report: one summary row per
//          assigned client (or just one client when filtered) and the most
//          popular activities. PopularActivityRow is also reused by the Admin reports.
// USED BY: ProgressController.TrainerReport and Views/Progress/TrainerReport.cshtml (Task 7).
// =====================================================================

namespace HealthTrack.ViewModels
{
    public class TrainerReportViewModel
    {
        public string? FilteredClientName { get; init; }                                                              // set when the report is for one client
        public IReadOnlyList<ClientPerformanceRow> Clients { get; init; } = Array.Empty<ClientPerformanceRow>();      // one row per client
        public IReadOnlyList<PopularActivityRow> PopularActivities { get; init; } = Array.Empty<PopularActivityRow>(); // most logged activities
    }

    // One client's totals in the trainer report
    public class ClientPerformanceRow
    {
        public string ClientId { get; init; } = string.Empty;   // used for the per-client link
        public string Name { get; init; } = string.Empty;       // client name
        public int WorkoutCount { get; init; }                  // workouts logged
        public int MealCount { get; init; }                     // meals logged
        public int WorkoutMinutes { get; init; }                // total minutes trained
        public int CaloriesBurned { get; init; }                // total calories burned
        public int WorkoutPlanCount { get; init; }              // workout plans they have
        public int DietPlanCount { get; init; }                 // diet plans they have
        public double? LatestWeightKg { get; init; }            // newest weight (null if none)
        public double? LatestBmi { get; init; }                 // newest BMI (null if none)
        public DateTime? LastActivity { get; init; }            // most recent log (null if none)
    }

    // One activity and how many times it was logged (used by trainer and admin reports)
    public class PopularActivityRow
    {
        public string Activity { get; init; } = string.Empty;   // e.g. "Running"
        public int Sessions { get; init; }                      // number of times logged
    }
}
