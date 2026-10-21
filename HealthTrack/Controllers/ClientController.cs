// =====================================================================
// FILE:    Controllers/ClientController.cs
// PURPOSE: The Client's own pages (Task 2):
//            - Index: the client dashboard (KPI cards, charts, plans, logs,
//              appointments, feedback)
//            - WorkoutPlans / DietPlans: lists of the client's own plans
//            - WorkoutPlan / DietPlan: one plan's details
// SECURITY: [Authorize(Roles = Client)] on the whole controller; every query is
//           filtered by the signed-in client's id, and a plan that belongs to
//           someone else returns 404; deactivated accounts are refused (Forbid).
// VIEWS:   Views/Client/*.cshtml
// =====================================================================

using HealthTrack.Models;                  // the model classes and AppRoles
using HealthTrack.Repositories.Interfaces; // IRepository<T>
using HealthTrack.ViewModels;              // ClientDashboardViewModel, ChartPoint, ...
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult, [HttpGet]

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Client)]                               // every action: clients only
    public class ClientController : Controller
    {
        // Repositories (data access) injected through the constructor
        private readonly IRepository<WorkoutPlan> _workoutPlans;       // workout plans
        private readonly IRepository<WorkoutPlanItem> _exercises;      // exercises inside plans
        private readonly IRepository<DietPlan> _dietPlans;             // diet plans
        private readonly IRepository<DietPlanItem> _plannedMeals;      // meals inside diet plans
        private readonly IRepository<WorkoutLog> _workouts;            // workouts logged
        private readonly IRepository<MealLog> _meals;                  // meals logged
        private readonly IRepository<HealthMeasurement> _measurements; // weight / height / BMI
        private readonly IRepository<Appointment> _appointments;       // appointments
        private readonly IRepository<Feedback> _feedback;              // trainer feedback
        private readonly IRepository<Notification> _notifications;     // notifications
        private readonly UserManager<ApplicationUser> _users;          // users

        // Constructor: ASP.NET Core passes in every dependency
        public ClientController(IRepository<WorkoutPlan> workoutPlans,
            IRepository<WorkoutPlanItem> exercises,
            IRepository<DietPlan> dietPlans,
            IRepository<DietPlanItem> plannedMeals,
            IRepository<WorkoutLog> workouts,
            IRepository<MealLog> meals,
            IRepository<HealthMeasurement> measurements,
            IRepository<Appointment> appointments,
            IRepository<Feedback> feedback,
            IRepository<Notification> notifications,
            UserManager<ApplicationUser> users)
        {
            _workoutPlans = workoutPlans;      // keep each dependency in a field
            _exercises = exercises;
            _dietPlans = dietPlans;
            _plannedMeals = plannedMeals;
            _workouts = workouts;
            _meals = meals;
            _measurements = measurements;
            _appointments = appointments;
            _feedback = feedback;
            _notifications = notifications;
            _users = users;
        }

        // GET: Client  -  the client dashboard
        public async Task<IActionResult> Index()
        {
            var clientId = _users.GetUserId(User);                     // the signed-in client's id
            if (clientId is null) return Challenge();                  // not signed in: go to login
            var user = await _users.FindByIdAsync(clientId);           // load the account
            if (user is not { IsActive: true }) return Forbid();       // deactivated accounts are refused

            // Load only this client's records
            var workouts = await _workouts.FindAsync(x => x.ClientId == clientId);
            var meals = await _meals.FindAsync(x => x.ClientId == clientId);
            var measurements = await _measurements.FindAsync(x => x.ClientId == clientId);
            var upcoming = await _appointments.FindAsync(x =>
                x.ClientId == clientId && x.AppointmentDate >= DateTime.Now &&                            // future appointments...
                (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Approved));        // ...still going ahead
            var feedback = await _feedback.FindAsync(x => x.ClientId == clientId);
            var notifications = await _notifications.FindAsync(x => x.UserId == clientId && !x.IsRead);  // unread only
            var workoutPlans = await _workoutPlans.FindAsync(x => x.ClientId == clientId);
            var dietPlans = await _dietPlans.FindAsync(x => x.ClientId == clientId);

            // ---- Chart data: worked out from the lists above, so only this client's records are used ----
            var today = DateTime.Today;                                                       // today at 00:00
            var week = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6)).ToList();   // last 7 days, oldest first
            var lastWeekStart = today.AddDays(-13);                                           // first day of the week before
            var thisWeekStart = today.AddDays(-6);                                            // first day of this week (7 days)

            var weights = measurements.OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();      // measurements, oldest first
            var latest = weights.LastOrDefault();                                             // newest measurement, or null

            return View(new ClientDashboardViewModel
            {
                // Weight line chart: one point per measurement
                WeightTrend = weights.Select(m => new ChartPoint { Label = m.Date.ToString("dd MMM"), Value = Math.Round(m.WeightKg, 1) }).ToList(),
                WeightChange = weights.Count > 1 ? Math.Round(weights[^1].WeightKg - weights[0].WeightKg, 1) : null,   // last minus first
                BmiCategory = latest is null ? null : BmiCategory(latest.BMI),                                          // e.g. "Healthy weight"
                // Weekly charts: one value per day for the last 7 days
                WeekLabels = week.Select(d => d.ToString("ddd")).ToList(),                                                         // "Mon", "Tue", ...
                WeekMinutes = week.Select(d => workouts.Where(w => w.Date.Date == d).Sum(w => w.DurationMinutes)).ToList(),        // minutes per day
                WeekBurned = week.Select(d => workouts.Where(w => w.Date.Date == d).Sum(w => w.CaloriesBurned)).ToList(),          // kcal burned per day
                WeekEaten = week.Select(d => meals.Where(m => m.Date.Date == d).Sum(m => m.Calories)).ToList(),                    // kcal eaten per day
                // "vs last week" figures on the KPI cards
                MinutesThisWeek = workouts.Where(w => w.Date.Date >= thisWeekStart).Sum(w => w.DurationMinutes),
                MinutesLastWeek = workouts.Where(w => w.Date.Date >= lastWeekStart && w.Date.Date < thisWeekStart).Sum(w => w.DurationMinutes),
                BurnedThisWeek = workouts.Where(w => w.Date.Date >= thisWeekStart).Sum(w => w.CaloriesBurned),
                BurnedLastWeek = workouts.Where(w => w.Date.Date >= lastWeekStart && w.Date.Date < thisWeekStart).Sum(w => w.CaloriesBurned),
                TopActivities = TopActivities(workouts.Where(w => w.Date.Date >= today.AddDays(-29))),                             // last 30 days

                // Lists shown on the dashboard
                Name = user.FirstName,                                                         // "Welcome back, Amal"
                WorkoutPlans = workoutPlans.OrderByDescending(x => x.CreatedAt).ToList(),      // newest first
                DietPlans = dietPlans.OrderByDescending(x => x.CreatedAt).ToList(),            // newest first
                RecentWorkouts = workouts.OrderByDescending(x => x.Date).Take(5).ToList(),     // last 5 workouts
                RecentMeals = meals.OrderByDescending(x => x.Date).Take(5).ToList(),           // last 5 meals
                LatestMeasurement = measurements.OrderByDescending(x => x.Date)                // newest measurement
                    .ThenByDescending(x => x.Id).FirstOrDefault(),
                UpcomingAppointments = upcoming.OrderBy(x => x.AppointmentDate).Take(5).ToList(),   // next 5, soonest first
                RecentFeedback = feedback.OrderByDescending(x => x.CreatedAt).Take(3).ToList(),     // latest 3
                UnreadNotifications = notifications.Count()                                         // bell number
            });
        }

        // GET: Client/WorkoutPlans
        // The navbar "Workouts" page: only the signed-in client's own workout plans.
        [HttpGet]
        public async Task<IActionResult> WorkoutPlans()
        {
            var clientId = _users.GetUserId(User);                     // the signed-in client's id
            if (clientId is null) return Challenge();                  // not signed in
            var user = await _users.FindByIdAsync(clientId);           // load the account
            if (user is not { IsActive: true }) return Forbid();       // deactivated accounts are refused
            var plans = await _workoutPlans.FindAsync(x => x.ClientId == clientId);                          // this client's plans only
            return View(plans.OrderByDescending(x => x.StartDate).ThenByDescending(x => x.Id).ToList());     // newest first
        }

        // GET: Client/DietPlans
        // The navbar "Diet" page: only the signed-in client's own diet plans.
        [HttpGet]
        public async Task<IActionResult> DietPlans()
        {
            var clientId = _users.GetUserId(User);                     // the signed-in client's id
            if (clientId is null) return Challenge();                  // not signed in
            var user = await _users.FindByIdAsync(clientId);           // load the account
            if (user is not { IsActive: true }) return Forbid();       // deactivated accounts are refused
            var plans = await _dietPlans.FindAsync(x => x.ClientId == clientId);                             // this client's plans only
            return View(plans.OrderByDescending(x => x.StartDate).ThenByDescending(x => x.Id).ToList());     // newest first
        }

        // WHO adult BMI bands, shown as text beside the BMI number on the dashboard
        private static string BmiCategory(double bmi) =>
            bmi < 18.5 ? "Underweight" : bmi < 25 ? "Healthy weight" : bmi < 30 ? "Overweight" : "Obese";

        // Number of sessions per activity name (case-insensitive), top 5, most logged first
        private static List<ChartPoint> TopActivities(IEnumerable<WorkoutLog> workouts) =>
            workouts.GroupBy(w => w.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)    // group "running" with "Running"
                .Select(g => new ChartPoint { Label = g.Key, Value = g.Count() })            // name + number of sessions
                .OrderByDescending(p => p.Value).ThenBy(p => p.Label)                        // most logged first
                .Take(5).ToList();                                                           // top 5 only

        // GET: Client/WorkoutPlan/5  -  one of the client's own workout plans
        [HttpGet]
        public async Task<IActionResult> WorkoutPlan(int id)
        {
            var clientId = _users.GetUserId(User);                     // the signed-in client's id
            if (clientId is null) return Challenge();                  // not signed in
            var user = await _users.FindByIdAsync(clientId);           // load the account
            if (user is not { IsActive: true }) return Forbid();       // deactivated accounts are refused
            var plan = await _workoutPlans.GetByIdAsync(id);           // load the plan
            if (plan is null || plan.ClientId != clientId) return NotFound();   // missing or someone else's: 404
            var exercises = await _exercises.FindAsync(x => x.WorkoutPlanId == id);   // the plan's exercises
            return View(new WorkoutPlanDetailsViewModel
            {
                Plan = plan,
                Exercises = exercises.OrderBy(x => x.DayOfWeek).ToList()    // Sunday to Saturday
            });
        }

        // GET: Client/DietPlan/5  -  one of the client's own diet plans
        [HttpGet]
        public async Task<IActionResult> DietPlan(int id)
        {
            var clientId = _users.GetUserId(User);                     // the signed-in client's id
            if (clientId is null) return Challenge();                  // not signed in
            var user = await _users.FindByIdAsync(clientId);           // load the account
            if (user is not { IsActive: true }) return Forbid();       // deactivated accounts are refused
            var plan = await _dietPlans.GetByIdAsync(id);              // load the plan
            if (plan is null || plan.ClientId != clientId) return NotFound();   // missing or someone else's: 404
            var meals = await _plannedMeals.FindAsync(x => x.DietPlanId == id);   // the plan's meals
            return View(new DietPlanDetailsViewModel
            {
                Plan = plan,
                Meals = meals.OrderBy(x => x.MealType).ThenBy(x => x.MealTime).ToList()   // breakfast first, then by time
            });
        }
    }
}
