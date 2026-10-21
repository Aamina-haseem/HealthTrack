// =====================================================================
// FILE:    Controllers/TrainerController.cs
// PURPOSE: The Trainer's own pages (Task 3):
//            - Index: the trainer dashboard (KPI cards, 14-day minutes chart,
//              popular activities, minutes by client, appointments, clients)
//            - Clients: the "My Clients" list
//            - ClientDetails: one assigned client's plans, logs and feedback
//            - GiveFeedback (GET + POST): write feedback for an assigned client
// SECURITY: [Authorize(Roles = Trainer)] on the whole controller; a trainer can
//           only see clients with an ACTIVE assignment to them (checked in
//           AssignedClientAsync / LoadAssignedClientsAsync), any other id gives 404;
//           ids for saving come from the server, never from the form.
// ERRORS:  the feedback save is wrapped in try/catch (DbUpdateException).
// VIEWS:   Views/Trainer/*.cshtml
// =====================================================================

using HealthTrack.Models;                  // the model classes and AppRoles
using HealthTrack.Repositories.Interfaces; // IRepository<T>
using HealthTrack.ViewModels;              // TrainerDashboardViewModel, AssignedClientSummary, ...
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult, [HttpGet], [HttpPost]
using Microsoft.EntityFrameworkCore;       // DbUpdateException

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Trainer)]                              // every action: trainers only
    public class TrainerController : Controller
    {
        // Repositories and services injected through the constructor
        private readonly IRepository<TrainerAssignment> _assignments;  // trainer <-> client links
        private readonly IRepository<WorkoutPlan> _workoutPlans;       // workout plans
        private readonly IRepository<DietPlan> _dietPlans;             // diet plans
        private readonly IRepository<WorkoutLog> _workouts;            // workouts logged
        private readonly IRepository<MealLog> _meals;                  // meals logged
        private readonly IRepository<HealthMeasurement> _measurements; // weight / height / BMI
        private readonly IRepository<Appointment> _appointments;       // appointments
        private readonly IRepository<Feedback> _feedback;              // feedback
        private readonly UserManager<ApplicationUser> _users;          // users and roles
        private readonly ILogger<TrainerController> _logger;           // writes errors to the log

        // Constructor: ASP.NET Core passes in every dependency
        public TrainerController(IRepository<TrainerAssignment> assignments,
            IRepository<WorkoutPlan> workoutPlans, IRepository<DietPlan> dietPlans,
            IRepository<WorkoutLog> workouts, IRepository<MealLog> meals,
            IRepository<HealthMeasurement> measurements,
            IRepository<Appointment> appointments, IRepository<Feedback> feedback,
            UserManager<ApplicationUser> users,
            ILogger<TrainerController> logger)
        {
            _assignments = assignments; _workoutPlans = workoutPlans;      // keep the dependencies...
            _dietPlans = dietPlans; _workouts = workouts; _meals = meals;  // ...in fields...
            _measurements = measurements; _appointments = appointments;    // ...for the actions...
            _feedback = feedback; _users = users;                          // ...below
            _logger = logger;
        }

        // GET: Trainer  -  the trainer dashboard
        public async Task<IActionResult> Index()
        {
            var trainerId = _users.GetUserId(User);                    // the signed-in trainer's id
            if (trainerId is null) return Challenge();                 // not signed in: go to login
            var trainer = await _users.FindByIdAsync(trainerId);       // load the account
            if (trainer is not { IsActive: true }) return Forbid();    // deactivated accounts are refused

            var clients = await LoadAssignedClientsAsync(trainerId);   // only this trainer's active clients
            var appointments = await _appointments.FindAsync(x =>
                x.TrainerId == trainerId && x.AppointmentDate >= DateTime.Now &&                            // future appointments...
                (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Approved));         // ...still going ahead
            var workoutPlans = await _workoutPlans.FindAsync(x => x.TrainerId == trainerId);                // plans this trainer wrote
            var dietPlans = await _dietPlans.FindAsync(x => x.TrainerId == trainerId);                      // diet plans this trainer wrote

            // ---- Chart data: only workouts of clients actively assigned to this trainer ----
            var today = DateTime.Today;                                // today at 00:00
            var monthStart = today.AddDays(-29);                       // start of the last 30 days
            var clientIds = clients.Select(c => c.ClientId).ToList();  // ids of the assigned clients
            var names = clients.ToDictionary(c => c.ClientId, c => c.Name);   // client name by id (not currently used below)
            var workouts = (await _workouts.FindAsync(w => clientIds.Contains(w.ClientId) && w.Date >= monthStart)).ToList();   // their last 30 days
            var days = Enumerable.Range(0, 14).Select(i => today.AddDays(i - 13)).ToList();   // last 14 days, oldest first

            return View(new TrainerDashboardViewModel
            {
                Name = trainer.FirstName, Clients = clients,                                       // greeting + clients table
                WorkoutPlanCount = workoutPlans.Count(), DietPlanCount = dietPlans.Count(),        // "Plans created" card
                UpcomingAppointments = appointments.OrderBy(x => x.AppointmentDate).Take(5).ToList(),   // next 5, soonest first
                DayLabels = days.Select(d => d.ToString("dd MMM")).ToList(),                       // x axis labels
                DailyMinutes = days.Select(d => workouts.Where(w => w.Date.Date == d).Sum(w => w.DurationMinutes)).ToList(),   // minutes per day
                WorkoutsThisWeek = workouts.Count(w => w.Date.Date >= today.AddDays(-6)),          // last 7 days
                WorkoutsLastWeek = workouts.Count(w => w.Date.Date >= today.AddDays(-13) && w.Date.Date < today.AddDays(-6)),   // the 7 days before
                PendingRequests = appointments.Count(a => a.Status == AppointmentStatus.Pending),  // "Requests to approve" card
                MinutesByClient = clients                                                          // one bar per client
                    .Select(c => new ChartPoint { Label = c.Name, Value = workouts.Where(w => w.ClientId == c.ClientId).Sum(w => w.DurationMinutes) })
                    .OrderByDescending(p => p.Value).ToList(),                                     // most active first
                TopActivities = workouts
                    .GroupBy(w => w.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)         // "running" = "Running"
                    .Select(g => new ChartPoint { Label = g.Key, Value = g.Count() })              // sessions per activity
                    .OrderByDescending(p => p.Value).ThenBy(p => p.Label).Take(5).ToList()         // top 5
            });
        }

        // GET: Trainer/Clients
        // The navbar "My Clients" page: every client actively assigned to this trainer.
        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            var trainerId = _users.GetUserId(User);                    // the signed-in trainer's id
            if (trainerId is null) return Challenge();                 // not signed in
            var trainer = await _users.FindByIdAsync(trainerId);       // load the account
            if (trainer is not { IsActive: true }) return Forbid();    // deactivated accounts are refused

            // Same list as the dashboard, so both pages always agree.
            var clients = await LoadAssignedClientsAsync(trainerId);
            return View(clients);                                      // show Views/Trainer/Clients.cshtml
        }

        // GET: Trainer/ClientDetails/{clientId}  -  one assigned client's progress
        [HttpGet]
        public async Task<IActionResult> ClientDetails(string id)
        {
            var trainerId = _users.GetUserId(User);                    // the signed-in trainer's id
            if (trainerId is null) return Challenge();                 // not signed in
            var trainer = await _users.FindByIdAsync(trainerId);       // load the account
            if (trainer is not { IsActive: true }) return Forbid();    // deactivated accounts are refused
            var client = await AssignedClientAsync(trainerId, id);     // the client, only if assigned to this trainer
            if (client is null) return NotFound();                     // not their client: 404

            // That client's records
            var workouts = await _workouts.FindAsync(x => x.ClientId == id);
            var meals = await _meals.FindAsync(x => x.ClientId == id);
            var measurements = await _measurements.FindAsync(x => x.ClientId == id);
            var feedback = await _feedback.FindAsync(x => x.ClientId == id && x.TrainerId == trainerId);   // only this trainer's feedback
            return View(new TrainerClientDetailsViewModel
            {
                Client = client,
                WorkoutPlans = (await _workoutPlans.FindAsync(x => x.ClientId == id && x.TrainerId == trainerId))   // plans by this trainer
                    .OrderByDescending(x => x.CreatedAt).ToList(),
                DietPlans = (await _dietPlans.FindAsync(x => x.ClientId == id && x.TrainerId == trainerId))         // plans by this trainer
                    .OrderByDescending(x => x.CreatedAt).ToList(),
                RecentWorkouts = workouts.OrderByDescending(x => x.Date).Take(5).ToList(),           // latest 5
                RecentMeals = meals.OrderByDescending(x => x.Date).Take(5).ToList(),                 // latest 5
                RecentMeasurements = measurements.OrderByDescending(x => x.Date).Take(5).ToList(),   // latest 5
                Feedback = feedback.OrderByDescending(x => x.CreatedAt).Take(5).ToList()             // latest 5
            });
        }

        // GET: Trainer/GiveFeedback/{clientId}  -  shows the empty feedback form
        [HttpGet]
        public async Task<IActionResult> GiveFeedback(string id)
        {
            var trainerId = _users.GetUserId(User);                    // the signed-in trainer's id
            if (trainerId is null) return Challenge();                 // not signed in
            var trainer = await _users.FindByIdAsync(trainerId);       // load the account
            if (trainer is not { IsActive: true }) return Forbid();    // deactivated accounts are refused
            var client = await AssignedClientAsync(trainerId, id);     // the client, only if assigned to this trainer
            if (client is null) return NotFound();                     // not their client: 404
            ViewBag.ClientId = client.Id;                              // used for the form's URL and Cancel link
            ViewBag.ClientName = client.FullName;                      // shown on the page
            return View(new FeedbackCreateViewModel());                // empty form
        }

        // POST: Trainer/GiveFeedback/{clientId}  -  saves the feedback
        [HttpPost, ValidateAntiForgeryToken]                           // POST only, with a valid anti-forgery token
        public async Task<IActionResult> GiveFeedback(string id, FeedbackCreateViewModel input)
        {
            var trainerId = _users.GetUserId(User);                    // the signed-in trainer's id
            if (trainerId is null) return Challenge();                 // not signed in
            var trainer = await _users.FindByIdAsync(trainerId);       // load the account
            if (trainer is not { IsActive: true }) return Forbid();    // deactivated accounts are refused
            var client = await AssignedClientAsync(trainerId, id);     // check the assignment again on POST
            if (client is null) return NotFound();                     // not their client: 404
            if (!ModelState.IsValid)                                   // validation failed
            {
                ViewBag.ClientId = client.Id;
                ViewBag.ClientName = client.FullName;
                return View(input);                                    // show the form again with the errors
            }
            // The client and trainer IDs are verified server-side, not bound from the form.
            await _feedback.AddAsync(new Feedback
            {
                ClientId = client.Id,                                  // from the verified assignment
                TrainerId = trainerId,                                 // from the signed-in user
                Message = input.Message.Trim(),                        // remove extra spaces
                Rating = input.Rating                                  // optional 1-5
            });
            try
            {
                await _feedback.SaveAsync();                           // write it to the database
            }
            catch (DbUpdateException ex)                               // the database save failed
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in TrainerController.GiveFeedback");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the feedback could not be saved. Please try again.");
                ViewBag.ClientId = client.Id;
                ViewBag.ClientName = client.FullName;
                return View(input);                                    // show the form again with the message
            }

            TempData["Success"] = "Feedback sent to the client.";      // green message on the next page
            return RedirectToAction(nameof(ClientDetails), new { id = client.Id });   // Post-Redirect-Get
        }

        // Builds the summary rows for the trainer's assigned clients.
        // Only active assignments to active users in the Client role are included,
        // so a trainer never sees anyone else's clients.
        private async Task<List<AssignedClientSummary>> LoadAssignedClientsAsync(string trainerId)
        {
            var assignments = await _assignments.FindAsync(a => a.TrainerId == trainerId && a.IsActive);   // active links only
            var clients = new List<AssignedClientSummary>();                                               // the result
            foreach (var clientId in assignments.Select(a => a.ClientId).Distinct())                       // each client once
            {
                var client = await _users.FindByIdAsync(clientId);                                         // load the user
                if (client is not { IsActive: true } ||                                                    // skip deactivated users...
                    !await _users.IsInRoleAsync(client, AppRoles.Client)) continue;                        // ...and non-clients
                var workouts = await _workouts.FindAsync(x => x.ClientId == clientId);                     // for the workout count
                var measurements = await _measurements.FindAsync(x => x.ClientId == clientId);             // for weight/BMI
                var latest = measurements.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).FirstOrDefault();   // newest one
                clients.Add(new AssignedClientSummary
                {
                    ClientId = clientId, Name = client.FullName,
                    WorkoutCount = workouts.Count(), LatestWeightKg = latest?.WeightKg,                    // null if none
                    LatestBmi = latest?.BMI
                });
            }
            return clients.OrderBy(x => x.Name).ToList();                                                  // alphabetical
        }

        // Returns the client only if they are actively assigned to this trainer,
        // are active and have the Client role; otherwise null (the caller returns 404).
        private async Task<ApplicationUser?> AssignedClientAsync(string trainerId, string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return null;                                  // no id given
            var assignment = await _assignments.FindAsync(a =>
                a.TrainerId == trainerId && a.ClientId == clientId && a.IsActive);                 // an active link?
            if (!assignment.Any()) return null;                                                    // not assigned
            var client = await _users.FindByIdAsync(clientId);                                     // load the user
            return client is { IsActive: true } &&
                await _users.IsInRoleAsync(client, AppRoles.Client) ? client : null;               // active client, or null
        }
    }
}
