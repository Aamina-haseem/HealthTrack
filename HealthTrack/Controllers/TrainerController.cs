using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Trainer)]
    public class TrainerController : Controller
    {
        private readonly IRepository<TrainerAssignment> _assignments;
        private readonly IRepository<WorkoutPlan> _workoutPlans;
        private readonly IRepository<DietPlan> _dietPlans;
        private readonly IRepository<WorkoutLog> _workouts;
        private readonly IRepository<MealLog> _meals;
        private readonly IRepository<HealthMeasurement> _measurements;
        private readonly IRepository<Appointment> _appointments;
        private readonly IRepository<Feedback> _feedback;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<TrainerController> _logger;

        public TrainerController(IRepository<TrainerAssignment> assignments,
            IRepository<WorkoutPlan> workoutPlans, IRepository<DietPlan> dietPlans,
            IRepository<WorkoutLog> workouts, IRepository<MealLog> meals,
            IRepository<HealthMeasurement> measurements,
            IRepository<Appointment> appointments, IRepository<Feedback> feedback,
            UserManager<ApplicationUser> users,
            ILogger<TrainerController> logger)
        {
            _assignments = assignments; _workoutPlans = workoutPlans;
            _dietPlans = dietPlans; _workouts = workouts; _meals = meals;
            _measurements = measurements; _appointments = appointments;
            _feedback = feedback; _users = users;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var trainerId = _users.GetUserId(User);
            if (trainerId is null) return Challenge();
            var trainer = await _users.FindByIdAsync(trainerId);
            if (trainer is not { IsActive: true }) return Forbid();

            var clients = await LoadAssignedClientsAsync(trainerId);
            var appointments = await _appointments.FindAsync(x =>
                x.TrainerId == trainerId && x.AppointmentDate >= DateTime.Now &&
                (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Approved));
            var workoutPlans = await _workoutPlans.FindAsync(x => x.TrainerId == trainerId);
            var dietPlans = await _dietPlans.FindAsync(x => x.TrainerId == trainerId);

            return View(new TrainerDashboardViewModel
            {
                Name = trainer.FirstName, Clients = clients,
                WorkoutPlanCount = workoutPlans.Count(), DietPlanCount = dietPlans.Count(),
                UpcomingAppointments = appointments.OrderBy(x => x.AppointmentDate).Take(5).ToList()
            });
        }

        // GET: Trainer/Clients
        // The navbar "My Clients" page: every client actively assigned to this trainer.
        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            var trainerId = _users.GetUserId(User);
            if (trainerId is null) return Challenge();
            var trainer = await _users.FindByIdAsync(trainerId);
            if (trainer is not { IsActive: true }) return Forbid();

            // Same list as the dashboard, so both pages always agree.
            var clients = await LoadAssignedClientsAsync(trainerId);
            return View(clients);
        }

        [HttpGet]
        public async Task<IActionResult> ClientDetails(string id)
        {
            var trainerId = _users.GetUserId(User);
            if (trainerId is null) return Challenge();
            var trainer = await _users.FindByIdAsync(trainerId);
            if (trainer is not { IsActive: true }) return Forbid();
            var client = await AssignedClientAsync(trainerId, id);
            if (client is null) return NotFound();

            var workouts = await _workouts.FindAsync(x => x.ClientId == id);
            var meals = await _meals.FindAsync(x => x.ClientId == id);
            var measurements = await _measurements.FindAsync(x => x.ClientId == id);
            var feedback = await _feedback.FindAsync(x => x.ClientId == id && x.TrainerId == trainerId);
            return View(new TrainerClientDetailsViewModel
            {
                Client = client,
                WorkoutPlans = (await _workoutPlans.FindAsync(x => x.ClientId == id && x.TrainerId == trainerId))
                    .OrderByDescending(x => x.CreatedAt).ToList(),
                DietPlans = (await _dietPlans.FindAsync(x => x.ClientId == id && x.TrainerId == trainerId))
                    .OrderByDescending(x => x.CreatedAt).ToList(),
                RecentWorkouts = workouts.OrderByDescending(x => x.Date).Take(5).ToList(),
                RecentMeals = meals.OrderByDescending(x => x.Date).Take(5).ToList(),
                RecentMeasurements = measurements.OrderByDescending(x => x.Date).Take(5).ToList(),
                Feedback = feedback.OrderByDescending(x => x.CreatedAt).Take(5).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> GiveFeedback(string id)
        {
            var trainerId = _users.GetUserId(User);
            if (trainerId is null) return Challenge();
            var trainer = await _users.FindByIdAsync(trainerId);
            if (trainer is not { IsActive: true }) return Forbid();
            var client = await AssignedClientAsync(trainerId, id);
            if (client is null) return NotFound();
            ViewBag.ClientId = client.Id;
            ViewBag.ClientName = client.FullName;
            return View(new FeedbackCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GiveFeedback(string id, FeedbackCreateViewModel input)
        {
            var trainerId = _users.GetUserId(User);
            if (trainerId is null) return Challenge();
            var trainer = await _users.FindByIdAsync(trainerId);
            if (trainer is not { IsActive: true }) return Forbid();
            var client = await AssignedClientAsync(trainerId, id);
            if (client is null) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.ClientId = client.Id;
                ViewBag.ClientName = client.FullName;
                return View(input);
            }
            // The client and trainer IDs are verified server-side, not bound from the form.
            await _feedback.AddAsync(new Feedback
            {
                ClientId = client.Id,
                TrainerId = trainerId,
                Message = input.Message.Trim(),
                Rating = input.Rating
            });
            try
            {
                await _feedback.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in TrainerController.GiveFeedback");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the feedback could not be saved. Please try again.");
                ViewBag.ClientId = client.Id;
                ViewBag.ClientName = client.FullName;
                return View(input);
            }

            TempData["Success"] = "Feedback sent to the client.";
            return RedirectToAction(nameof(ClientDetails), new { id = client.Id });
        }

        // Builds the summary rows for the trainer's assigned clients.
        // Only active assignments to active users in the Client role are included,
        // so a trainer never sees anyone else's clients.
        private async Task<List<AssignedClientSummary>> LoadAssignedClientsAsync(string trainerId)
        {
            var assignments = await _assignments.FindAsync(a => a.TrainerId == trainerId && a.IsActive);
            var clients = new List<AssignedClientSummary>();
            foreach (var clientId in assignments.Select(a => a.ClientId).Distinct())
            {
                var client = await _users.FindByIdAsync(clientId);
                if (client is not { IsActive: true } ||
                    !await _users.IsInRoleAsync(client, AppRoles.Client)) continue;
                var workouts = await _workouts.FindAsync(x => x.ClientId == clientId);
                var measurements = await _measurements.FindAsync(x => x.ClientId == clientId);
                var latest = measurements.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).FirstOrDefault();
                clients.Add(new AssignedClientSummary
                {
                    ClientId = clientId, Name = client.FullName,
                    WorkoutCount = workouts.Count(), LatestWeightKg = latest?.WeightKg,
                    LatestBmi = latest?.BMI
                });
            }
            return clients.OrderBy(x => x.Name).ToList();
        }

        private async Task<ApplicationUser?> AssignedClientAsync(string trainerId, string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return null;
            var assignment = await _assignments.FindAsync(a =>
                a.TrainerId == trainerId && a.ClientId == clientId && a.IsActive);
            if (!assignment.Any()) return null;
            var client = await _users.FindByIdAsync(clientId);
            return client is { IsActive: true } &&
                await _users.IsInRoleAsync(client, AppRoles.Client) ? client : null;
        }
    }
}
