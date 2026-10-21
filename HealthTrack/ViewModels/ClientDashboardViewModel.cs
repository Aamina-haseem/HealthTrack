// =====================================================================
// FILE:    ViewModels/ClientDashboardViewModel.cs
// PURPOSE: Everything the Client dashboard shows: the client's plans, recent logs,
//          latest measurement, appointments, feedback, and the data for the KPI
//          cards and charts (weight trend, weekly minutes/calories, top activities).
// KEY IDEA: built only from the signed-in client's own records, so no other
//           user's data can ever reach this page.
// USED BY: ClientController.Index and Views/Client/Index.cshtml (Task 2).
// =====================================================================

using HealthTrack.Models;   // WorkoutPlan, DietPlan, WorkoutLog, MealLog, ...
namespace HealthTrack.ViewModels
{
    // One view model for the client's own dashboard; no other user's rows are loaded.
    public class ClientDashboardViewModel
    {
        public string Name { get; init; } = string.Empty;                                                         // for "Welcome back, ..."
        public IReadOnlyList<WorkoutPlan> WorkoutPlans { get; init; } = Array.Empty<WorkoutPlan>();               // plans from the trainer
        public IReadOnlyList<DietPlan> DietPlans { get; init; } = Array.Empty<DietPlan>();                        // diet plans from the trainer
        public IReadOnlyList<WorkoutLog> RecentWorkouts { get; init; } = Array.Empty<WorkoutLog>();               // latest workouts table
        public IReadOnlyList<MealLog> RecentMeals { get; init; } = Array.Empty<MealLog>();                        // latest meals
        public HealthMeasurement? LatestMeasurement { get; init; }                                                // newest weight/BMI (null if none)
        public IReadOnlyList<Appointment> UpcomingAppointments { get; init; } = Array.Empty<Appointment>();       // appointments still to come
        public IReadOnlyList<Feedback> RecentFeedback { get; init; } = Array.Empty<Feedback>();                   // latest trainer feedback
        public int UnreadNotifications { get; init; }                                                             // bell count

        // ---- Chart data (built in ClientController from this client's own records) ----

        // Weight at every measurement, oldest first (line chart + sparkline)
        public IReadOnlyList<ChartPoint> WeightTrend { get; init; } = Array.Empty<ChartPoint>();
        // Latest weight minus the first recorded weight (kg); null until there are two measurements
        public double? WeightChange { get; init; }
        // WHO adult BMI band for the latest measurement, e.g. "Healthy weight"
        public string? BmiCategory { get; init; }

        // The last 7 days (oldest first) with totals per day
        public IReadOnlyList<string> WeekLabels { get; init; } = Array.Empty<string>();   // day names for the x axis
        public IReadOnlyList<int> WeekMinutes { get; init; } = Array.Empty<int>();        // workout minutes per day
        public IReadOnlyList<int> WeekBurned { get; init; } = Array.Empty<int>();         // calories burned per day
        public IReadOnlyList<int> WeekEaten { get; init; } = Array.Empty<int>();          // calories eaten per day

        // Totals for this week and the week before (for the "vs last week" text)
        public int MinutesThisWeek { get; init; }       // workout minutes, last 7 days
        public int MinutesLastWeek { get; init; }       // workout minutes, the 7 days before that
        public int BurnedThisWeek { get; init; }        // calories burned, last 7 days
        public int BurnedLastWeek { get; init; }        // calories burned, the 7 days before that

        // Most logged activities in the last 30 days (sessions per exercise)
        public IReadOnlyList<ChartPoint> TopActivities { get; init; } = Array.Empty<ChartPoint>();
    }
}
