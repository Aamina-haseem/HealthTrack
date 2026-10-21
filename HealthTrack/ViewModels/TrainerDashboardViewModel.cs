// =====================================================================
// FILE:    ViewModels/TrainerDashboardViewModel.cs
// PURPOSE: Everything the Trainer dashboard shows: the trainer's assigned clients
//          (with their latest weight/BMI), plan counts, upcoming appointments and
//          the data for the KPI cards and charts (14-day minutes, weekly
//          workouts, pending requests, minutes per client, popular activities).
// KEY IDEA: only clients ACTIVELY assigned to this trainer are included.
// USED BY: TrainerController.Index and Views/Trainer/Index.cshtml (Task 3).
// =====================================================================

using HealthTrack.Models;   // Appointment
namespace HealthTrack.ViewModels
{
    public class TrainerDashboardViewModel
    {
        public string Name { get; init; } = string.Empty;                                                           // for "Welcome back, ..."
        public IReadOnlyList<AssignedClientSummary> Clients { get; init; } = Array.Empty<AssignedClientSummary>();  // one row per assigned client
        public int WorkoutPlanCount { get; init; }                                                                  // workout plans this trainer wrote
        public int DietPlanCount { get; init; }                                                                     // diet plans this trainer wrote
        public IReadOnlyList<Appointment> UpcomingAppointments { get; init; } = Array.Empty<Appointment>();         // appointments still to come

        // ---- Chart data (only clients actively assigned to this trainer) ----

        // The last 14 days (oldest first) and the workout minutes all assigned clients logged each day
        public IReadOnlyList<string> DayLabels { get; init; } = Array.Empty<string>();   // x axis: dates
        public IReadOnlyList<int> DailyMinutes { get; init; } = Array.Empty<int>();      // y axis: minutes per day

        // Workouts logged by assigned clients this week and the week before
        public int WorkoutsThisWeek { get; init; }
        public int WorkoutsLastWeek { get; init; }

        // Appointment requests still waiting for this trainer's approval
        public int PendingRequests { get; init; }

        // Workout minutes per client and most logged activities, both for the last 30 days
        public IReadOnlyList<ChartPoint> MinutesByClient { get; init; } = Array.Empty<ChartPoint>();
        public IReadOnlyList<ChartPoint> TopActivities { get; init; } = Array.Empty<ChartPoint>();
    }

    // One row in the "Assigned Clients" table
    public class AssignedClientSummary
    {
        public string ClientId { get; init; } = string.Empty;   // used by the "View Progress" link
        public string Name { get; init; } = string.Empty;       // client name
        public double? LatestWeightKg { get; init; }            // newest weight (null if none recorded)
        public double? LatestBmi { get; init; }                 // newest BMI (null if none recorded)
        public int WorkoutCount { get; init; }                  // total workouts logged
    }
}
