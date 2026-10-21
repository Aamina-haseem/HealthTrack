// =====================================================================
// FILE:    Controllers/ProgressController.cs
// PURPOSE: Progress tracking and reports (Task 7):
//            - Index (clients): the client's own weight/BMI and activity charts
//              plus 30-day totals.
//            - Reports (trainers): one performance row per ASSIGNED client and the
//              most popular activities; can be filtered to one client.
// SECURITY: role-based [Authorize]; a trainer can only report on clients with an
//           active assignment to them (any other id returns 404); deactivated
//           accounts are refused (Forbid).
// VIEWS:   Views/Progress/Index.cshtml, Views/Progress/TrainerReport.cshtml
// =====================================================================

using HealthTrack.Models;                  // the model classes and AppRoles
using HealthTrack.Repositories.Interfaces; // IRepository<T>
using HealthTrack.ViewModels;              // ProgressDashboardViewModel, TrainerReportViewModel, ...
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult

namespace HealthTrack.Controllers
{
    [Authorize]                                                        // must be signed in (each action narrows the role)
    public class ProgressController : Controller
    {
        // Repositories (data access) injected through the constructor
        private readonly IRepository<HealthMeasurement> _measurements; // weight / height / BMI
        private readonly IRepository<WorkoutLog> _workouts;            // workouts logged
        private readonly IRepository<MealLog> _meals;                  // meals logged
        private readonly IRepository<WorkoutPlan> _workoutPlans;       // workout plans
        private readonly IRepository<DietPlan> _dietPlans;             // diet plans
        private readonly IRepository<TrainerAssignment> _assignments;  // trainer <-> client links
        private readonly UserManager<ApplicationUser> _users;          // users and roles

        // Constructor: ASP.NET Core passes in every dependency
        public ProgressController(IRepository<HealthMeasurement> measurements,
            IRepository<WorkoutLog> workouts, IRepository<MealLog> meals,
            IRepository<WorkoutPlan> workoutPlans, IRepository<DietPlan> dietPlans,
            IRepository<TrainerAssignment> assignments, UserManager<ApplicationUser> users)
        {
            _measurements = measurements; _workouts = workouts; _meals = meals;   // keep the repositories...
            _workoutPlans = workoutPlans; _dietPlans = dietPlans;                 // ...in fields...
            _assignments = assignments; _users = users;                           // ...for the actions below
        }

        // GET: Progress  -  the client's own progress page
        [Authorize(Roles = AppRoles.Client)]                           // clients only
        public async Task<IActionResult> Index()
        {
            var clientId = _users.GetUserId(User);                     // the signed-in client's id
            if (clientId is null) return Challenge();                  // not signed in: go to login
            var client = await _users.FindByIdAsync(clientId);         // load the account
            if (client is not { IsActive: true }) return Forbid();     // deactivated accounts are refused

            // This client's records only, oldest first (charts read left to right)
            var measurements = (await _measurements.FindAsync(x => x.ClientId == clientId))
                .OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();
            var workouts = (await _workouts.FindAsync(x => x.ClientId == clientId))
                .OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();
            var meals = (await _meals.FindAsync(x => x.ClientId == clientId))
                .OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();

            var since = DateTime.Today.AddDays(-29);                   // start of the last 30 days (today included)
            // All trend data is taken from this client's own records.
            return View(new ProgressDashboardViewModel
            {
                Measurements = measurements,                           // for the weight/BMI charts
                Workouts = workouts,                                   // for the activity chart
                Meals = meals,                                         // for the calories chart
                WeightChangeKg = measurements.Count > 1                // needs at least two measurements
                    ? Math.Round(measurements[^1].WeightKg - measurements[0].WeightKg, 1) : null,   // last minus first
                CaloriesBurnedLast30Days = workouts.Where(x => x.Date.Date >= since).Sum(x => x.CaloriesBurned),     // 30-day total
                CaloriesConsumedLast30Days = meals.Where(x => x.Date.Date >= since).Sum(x => x.Calories),            // 30-day total
                WorkoutMinutesLast30Days = workouts.Where(x => x.Date.Date >= since).Sum(x => x.DurationMinutes)     // 30-day total
            });
        }

        // GET: Progress/Reports  or  Progress/Reports/{clientId}  -  the trainer's report
        [Authorize(Roles = AppRoles.Trainer)]                          // trainers only
        public async Task<IActionResult> Reports(string? id)           // id = optional client filter
        {
            var trainerId = _users.GetUserId(User);                    // the signed-in trainer's id
            if (trainerId is null) return Challenge();                 // not signed in
            var trainer = await _users.FindByIdAsync(trainerId);       // load the account
            if (trainer is not { IsActive: true }) return Forbid();    // deactivated accounts are refused

            // The clients this trainer may see: only ACTIVE assignments
            var assignments = await _assignments.FindAsync(a =>
                a.TrainerId == trainerId && a.IsActive);
            var allowedIds = assignments.Select(a => a.ClientId).Distinct().ToList();
            if (id is not null && !allowedIds.Contains(id)) return NotFound();   // someone else's client: 404

            var report = new List<ClientPerformanceRow>();             // one row per client
            var allWorkouts = new List<WorkoutLog>();                  // collected for "popular activities"
            foreach (var clientId in allowedIds.Where(x => id is null || x == id))   // all clients, or just the filtered one
            {
                var client = await _users.FindByIdAsync(clientId);     // load the client
                if (client is not { IsActive: true } ||                // skip deactivated accounts...
                    !await _users.IsInRoleAsync(client, AppRoles.Client)) continue;   // ...and anyone who is not a client

                // This client's records
                var workouts = (await _workouts.FindAsync(x => x.ClientId == clientId)).ToList();
                allWorkouts.AddRange(workouts);                        // add to the overall list
                var meals = (await _meals.FindAsync(x => x.ClientId == clientId)).ToList();
                var measurements = (await _measurements.FindAsync(x => x.ClientId == clientId)).ToList();
                var workoutPlans = await _workoutPlans.FindAsync(x => x.TrainerId == trainerId && x.ClientId == clientId);   // plans by THIS trainer
                var dietPlans = await _dietPlans.FindAsync(x => x.TrainerId == trainerId && x.ClientId == clientId);         // plans by THIS trainer
                var latest = measurements.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).FirstOrDefault();       // newest measurement
                var dates = workouts.Select(x => x.Date).Concat(meals.Select(x => x.Date))     // every date the client
                    .Concat(measurements.Select(x => x.Date)).ToList();                        // logged anything
                report.Add(new ClientPerformanceRow
                {
                    ClientId = clientId, Name = client.FullName,                                // who
                    WorkoutCount = workouts.Count, MealCount = meals.Count,                     // how many logs
                    WorkoutMinutes = workouts.Sum(x => x.DurationMinutes),                      // total minutes
                    CaloriesBurned = workouts.Sum(x => x.CaloriesBurned),                       // total calories
                    WorkoutPlanCount = workoutPlans.Count(), DietPlanCount = dietPlans.Count(), // plans from this trainer
                    LatestWeightKg = latest?.WeightKg, LatestBmi = latest?.BMI,                 // null if no measurement
                    LastActivity = dates.Count > 0 ? dates.Max() : null                         // most recent date, or null
                });
            }
            return View(new TrainerReportViewModel
            {
                FilteredClientName = id is null ? null : report.FirstOrDefault()?.Name,        // heading when filtered
                Clients = report.OrderBy(x => x.Name).ToList(),                                 // alphabetical
                PopularActivities = allWorkouts
                    .GroupBy(x => x.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)      // "running" = "Running"
                    .Select(group => new PopularActivityRow { Activity = group.Key, Sessions = group.Count() })   // count each
                    .OrderByDescending(x => x.Sessions).ThenBy(x => x.Activity).Take(5).ToList()                  // top 5
            });
        }
    }
}
