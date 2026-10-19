using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Client)]
    public class ClientController : Controller
    {
        private readonly IRepository<WorkoutPlan> _workoutPlans;
        private readonly IRepository<WorkoutPlanItem> _exercises;
        private readonly IRepository<DietPlan> _dietPlans;
        private readonly IRepository<DietPlanItem> _plannedMeals;
        private readonly IRepository<WorkoutLog> _workouts;
        private readonly IRepository<MealLog> _meals;
        private readonly IRepository<HealthMeasurement> _measurements;
        private readonly IRepository<Appointment> _appointments;
        private readonly IRepository<Feedback> _feedback;
        private readonly IRepository<Notification> _notifications;
        private readonly UserManager<ApplicationUser> _users;

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
            _workoutPlans = workoutPlans;
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

        public async Task<IActionResult> Index()
        {
            var clientId = _users.GetUserId(User);
            if (clientId is null) return Challenge();
            var user = await _users.FindByIdAsync(clientId);
            if (user is not { IsActive: true }) return Forbid();

            var workouts = await _workouts.FindAsync(x => x.ClientId == clientId);
            var meals = await _meals.FindAsync(x => x.ClientId == clientId);
            var measurements = await _measurements.FindAsync(x => x.ClientId == clientId);
            var upcoming = await _appointments.FindAsync(x =>
                x.ClientId == clientId && x.AppointmentDate >= DateTime.Now &&
                (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Approved));
            var feedback = await _feedback.FindAsync(x => x.ClientId == clientId);
            var notifications = await _notifications.FindAsync(x => x.UserId == clientId && !x.IsRead);
            var workoutPlans = await _workoutPlans.FindAsync(x => x.ClientId == clientId);
            var dietPlans = await _dietPlans.FindAsync(x => x.ClientId == clientId);

            return View(new ClientDashboardViewModel
            {
                Name = user.FirstName,
                WorkoutPlans = workoutPlans.OrderByDescending(x => x.CreatedAt).ToList(),
                DietPlans = dietPlans.OrderByDescending(x => x.CreatedAt).ToList(),
                RecentWorkouts = workouts.OrderByDescending(x => x.Date).Take(5).ToList(),
                RecentMeals = meals.OrderByDescending(x => x.Date).Take(5).ToList(),
                LatestMeasurement = measurements.OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.Id).FirstOrDefault(),
                UpcomingAppointments = upcoming.OrderBy(x => x.AppointmentDate).Take(5).ToList(),
                RecentFeedback = feedback.OrderByDescending(x => x.CreatedAt).Take(3).ToList(),
                UnreadNotifications = notifications.Count()
            });
        }

        [HttpGet]
        public async Task<IActionResult> WorkoutPlan(int id)
        {
            var clientId = _users.GetUserId(User);
            if (clientId is null) return Challenge();
            var user = await _users.FindByIdAsync(clientId);
            if (user is not { IsActive: true }) return Forbid();
            var plan = await _workoutPlans.GetByIdAsync(id);
            if (plan is null || plan.ClientId != clientId) return NotFound();
            var exercises = await _exercises.FindAsync(x => x.WorkoutPlanId == id);
            return View(new WorkoutPlanDetailsViewModel
            {
                Plan = plan,
                Exercises = exercises.OrderBy(x => x.DayOfWeek).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> DietPlan(int id)
        {
            var clientId = _users.GetUserId(User);
            if (clientId is null) return Challenge();
            var user = await _users.FindByIdAsync(clientId);
            if (user is not { IsActive: true }) return Forbid();
            var plan = await _dietPlans.GetByIdAsync(id);
            if (plan is null || plan.ClientId != clientId) return NotFound();
            var meals = await _plannedMeals.FindAsync(x => x.DietPlanId == id);
            return View(new DietPlanDetailsViewModel
            {
                Plan = plan,
                Meals = meals.OrderBy(x => x.MealType).ThenBy(x => x.MealTime).ToList()
            });
        }
    }
}
