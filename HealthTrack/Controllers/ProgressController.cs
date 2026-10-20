using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    [Authorize]
    public class ProgressController : Controller
    {
        private readonly IRepository<HealthMeasurement> _measurements;
        private readonly IRepository<WorkoutLog> _workouts;
        private readonly IRepository<MealLog> _meals;
        private readonly IRepository<WorkoutPlan> _workoutPlans;
        private readonly IRepository<DietPlan> _dietPlans;
        private readonly IRepository<TrainerAssignment> _assignments;
        private readonly UserManager<ApplicationUser> _users;

        public ProgressController(IRepository<HealthMeasurement> measurements,
            IRepository<WorkoutLog> workouts, IRepository<MealLog> meals,
            IRepository<WorkoutPlan> workoutPlans, IRepository<DietPlan> dietPlans,
            IRepository<TrainerAssignment> assignments, UserManager<ApplicationUser> users)
        {
            _measurements = measurements; _workouts = workouts; _meals = meals;
            _workoutPlans = workoutPlans; _dietPlans = dietPlans;
            _assignments = assignments; _users = users;
        }

        [Authorize(Roles = AppRoles.Client)]
        public async Task<IActionResult> Index()
        {
            var clientId = _users.GetUserId(User);
            if (clientId is null) return Challenge();
            var client = await _users.FindByIdAsync(clientId);
            if (client is not { IsActive: true }) return Forbid();

            var measurements = (await _measurements.FindAsync(x => x.ClientId == clientId))
                .OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();
            var workouts = (await _workouts.FindAsync(x => x.ClientId == clientId))
                .OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();
            var meals = (await _meals.FindAsync(x => x.ClientId == clientId))
                .OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();

            var since = DateTime.Today.AddDays(-29);
            // All trend data is taken from this client's own records.
            return View(new ProgressDashboardViewModel
            {
                Measurements = measurements,
                Workouts = workouts,
                Meals = meals,
                WeightChangeKg = measurements.Count > 1
                    ? Math.Round(measurements[^1].WeightKg - measurements[0].WeightKg, 1) : null,
                CaloriesBurnedLast30Days = workouts.Where(x => x.Date.Date >= since).Sum(x => x.CaloriesBurned),
                CaloriesConsumedLast30Days = meals.Where(x => x.Date.Date >= since).Sum(x => x.Calories),
                WorkoutMinutesLast30Days = workouts.Where(x => x.Date.Date >= since).Sum(x => x.DurationMinutes)
            });
        }

        [Authorize(Roles = AppRoles.Trainer)]
        public async Task<IActionResult> Reports(string? id)
        {
            var trainerId = _users.GetUserId(User);
            if (trainerId is null) return Challenge();
            var trainer = await _users.FindByIdAsync(trainerId);
            if (trainer is not { IsActive: true }) return Forbid();

            var assignments = await _assignments.FindAsync(a =>
                a.TrainerId == trainerId && a.IsActive);
            var allowedIds = assignments.Select(a => a.ClientId).Distinct().ToList();
            if (id is not null && !allowedIds.Contains(id)) return NotFound();

            var report = new List<ClientPerformanceRow>();
            var allWorkouts = new List<WorkoutLog>();
            foreach (var clientId in allowedIds.Where(x => id is null || x == id))
            {
                var client = await _users.FindByIdAsync(clientId);
                if (client is not { IsActive: true } ||
                    !await _users.IsInRoleAsync(client, AppRoles.Client)) continue;

                var workouts = (await _workouts.FindAsync(x => x.ClientId == clientId)).ToList();
                allWorkouts.AddRange(workouts);
                var meals = (await _meals.FindAsync(x => x.ClientId == clientId)).ToList();
                var measurements = (await _measurements.FindAsync(x => x.ClientId == clientId)).ToList();
                var workoutPlans = await _workoutPlans.FindAsync(x => x.TrainerId == trainerId && x.ClientId == clientId);
                var dietPlans = await _dietPlans.FindAsync(x => x.TrainerId == trainerId && x.ClientId == clientId);
                var latest = measurements.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).FirstOrDefault();
                var dates = workouts.Select(x => x.Date).Concat(meals.Select(x => x.Date))
                    .Concat(measurements.Select(x => x.Date)).ToList();
                report.Add(new ClientPerformanceRow
                {
                    ClientId = clientId, Name = client.FullName,
                    WorkoutCount = workouts.Count, MealCount = meals.Count,
                    WorkoutMinutes = workouts.Sum(x => x.DurationMinutes),
                    CaloriesBurned = workouts.Sum(x => x.CaloriesBurned),
                    WorkoutPlanCount = workoutPlans.Count(), DietPlanCount = dietPlans.Count(),
                    LatestWeightKg = latest?.WeightKg, LatestBmi = latest?.BMI,
                    LastActivity = dates.Count > 0 ? dates.Max() : null
                });
            }
            return View(new TrainerReportViewModel
            {
                FilteredClientName = id is null ? null : report.FirstOrDefault()?.Name,
                Clients = report.OrderBy(x => x.Name).ToList(),
                PopularActivities = allWorkouts
                    .GroupBy(x => x.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => new PopularActivityRow { Activity = group.Key, Sessions = group.Count() })
                    .OrderByDescending(x => x.Sessions).ThenBy(x => x.Activity).Take(5).ToList()
            });
        }
    }
}
