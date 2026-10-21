using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.Services;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    /// <summary>
    /// Task 4 - Administrator module (Admins only).
    ///  Dashboard:   user activity, system usage and trainer assignments at a glance
    ///  Users:       search users, create Trainer accounts, activate / deactivate accounts
    ///  Assignments: assign clients to trainers (one active trainer per client)
    ///  Reports:     client engagement, popular routines and trainer performance
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly ITrainerAssignmentRepository _assignments;
        private readonly IAppointmentRepository _appointments;
        private readonly IRepository<WorkoutLog> _workoutLogs;
        private readonly IRepository<MealLog> _mealLogs;
        private readonly IRepository<HealthMeasurement> _measurements;
        private readonly IRepository<WorkoutPlan> _workoutPlans;
        private readonly IRepository<DietPlan> _dietPlans;
        private readonly IRepository<Feedback> _feedback;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminController> _logger;

        // Specializations offered when creating a trainer / health professional
        private static readonly string[] Specializations =
        {
            "Fitness Trainer", "Personal Trainer", "Nutritionist", "Dietitian",
            "Physiotherapist", "Yoga Instructor", "Doctor"
        };

        public AdminController(
            UserManager<ApplicationUser> users,
            ITrainerAssignmentRepository assignments,
            IAppointmentRepository appointments,
            IRepository<WorkoutLog> workoutLogs,
            IRepository<MealLog> mealLogs,
            IRepository<HealthMeasurement> measurements,
            IRepository<WorkoutPlan> workoutPlans,
            IRepository<DietPlan> dietPlans,
            IRepository<Feedback> feedback,
            INotificationService notificationService,
            ILogger<AdminController> logger)
        {
            _users = users;
            _assignments = assignments;
            _appointments = appointments;
            _workoutLogs = workoutLogs;
            _mealLogs = mealLogs;
            _measurements = measurements;
            _workoutPlans = workoutPlans;
            _dietPlans = dietPlans;
            _feedback = feedback;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ===============================================================
        // DASHBOARD
        // ===============================================================

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            var admin = await _users.GetUserAsync(User);
            if (admin is null) return Challenge();

            DateTime today = DateTime.Today;
            DateTime weekStart = today.AddDays(-6);       // last 7 days including today
            DateTime monthStart = today.AddDays(-29);     // last 30 days including today

            var clients = await _users.GetUsersInRoleAsync(AppRoles.Client);
            var trainers = await _users.GetUsersInRoleAsync(AppRoles.Trainer);
            var allUsers = await _users.Users.ToListAsync();
            var activeAssignments = (await _assignments.FindAsync(a => a.IsActive)).ToList();
            var appointments = (await _appointments.GetAllAsync()).ToList();

            var workoutsThisWeek = (await _workoutLogs.FindAsync(w => w.Date >= weekStart)).ToList();
            var mealsThisWeek = (await _mealLogs.FindAsync(m => m.Date >= weekStart)).ToList();
            var measurementsThisWeek = (await _measurements.FindAsync(h => h.Date >= weekStart)).ToList();

            // Activity per day for the chart (oldest day first)
            var labels = new List<string>();
            var counts = new List<int>();
            for (DateTime day = weekStart; day <= today; day = day.AddDays(1))
            {
                labels.Add(day.ToString("ddd dd"));
                counts.Add(workoutsThisWeek.Count(w => w.Date.Date == day)
                         + mealsThisWeek.Count(m => m.Date.Date == day)
                         + measurementsThisWeek.Count(h => h.Date.Date == day));
            }

            // Clients per trainer
            var workload = trainers
                .Select(t => new TrainerWorkloadRow
                {
                    TrainerId = t.Id,
                    Name = t.FullName,
                    Specialization = t.Specialization,
                    IsActive = t.IsActive,
                    ClientCount = activeAssignments.Count(a => a.TrainerId == t.Id),
                    UpcomingAppointments = appointments.Count(a => a.TrainerId == t.Id &&
                        a.AppointmentDate >= DateTime.Now &&
                        (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved))
                })
                .OrderByDescending(t => t.ClientCount).ThenBy(t => t.Name)
                .ToList();

            var assignedClientIds = activeAssignments.Select(a => a.ClientId).ToHashSet();

            var model = new AdminDashboardViewModel
            {
                Name = admin.FullName,          // full name, e.g. "System Admin"
                TotalClients = clients.Count,
                TotalTrainers = trainers.Count,
                ActiveUsers = allUsers.Count(u => u.IsActive),
                InactiveUsers = allUsers.Count(u => !u.IsActive),
                NewUsersLast30Days = allUsers.Count(u => u.CreatedAt >= monthStart),
                UnassignedClients = clients.Count(c => c.IsActive && !assignedClientIds.Contains(c.Id)),
                PendingAppointments = appointments.Count(a => a.Status == AppointmentStatus.Pending),
                UpcomingAppointments = appointments.Count(a => a.AppointmentDate >= DateTime.Now &&
                    (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved)),
                CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                WorkoutLogsLast7Days = workoutsThisWeek.Count,
                MealLogsLast7Days = mealsThisWeek.Count,
                MeasurementsLast7Days = measurementsThisWeek.Count,
                ActivityLabels = labels,
                ActivityCounts = counts,
                TrainerWorkload = workload,
                RecentUsers = (await BuildUserRowsAsync(allUsers))
                    .OrderByDescending(u => u.CreatedAt).Take(5).ToList()
            };

            return View(model);
        }

        // ===============================================================
        // USERS
        // ===============================================================

        // GET: Admin/Users?role=Trainer&search=kamal
        public async Task<IActionResult> Users(string? role, string? search)
        {
            var allUsers = await _users.Users.ToListAsync();
            var rows = await BuildUserRowsAsync(allUsers);

            // Filter by role
            if (!string.IsNullOrEmpty(role))
            {
                rows = rows.Where(u => u.Role == role).ToList();
            }

            // Search by name or email (case-insensitive)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                rows = rows.Where(u =>
                        u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return View(new AdminUsersViewModel
            {
                Users = rows.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToList(),
                RoleFilter = role,
                Search = search,
                CurrentAdminId = _users.GetUserId(User) ?? string.Empty
            });
        }

        // GET: Admin/CreateTrainer
        public IActionResult CreateTrainer()
        {
            ViewBag.Specializations = new SelectList(Specializations);
            return View(new CreateTrainerViewModel());
        }

        // POST: Admin/CreateTrainer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(CreateTrainerViewModel input)
        {
            if (!Specializations.Contains(input.Specialization))
            {
                ModelState.AddModelError(nameof(input.Specialization), "Please choose a specialization from the list.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Specializations = new SelectList(Specializations, input.Specialization);
                return View(input);
            }

            try
            {
                var trainer = new ApplicationUser
                {
                    UserName = input.Email.Trim(),
                    Email = input.Email.Trim(),
                    EmailConfirmed = true,
                    FirstName = input.FirstName.Trim(),
                    LastName = input.LastName.Trim(),
                    Gender = input.Gender,
                    Specialization = input.Specialization,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                // Identity hashes the password and checks the password rules
                var result = await _users.CreateAsync(trainer, input.Password);
                if (result.Succeeded)
                {
                    var roleResult = await _users.AddToRoleAsync(trainer, AppRoles.Trainer);
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("Admin created trainer account {Email}", trainer.Email);
                        await NotifySafelyAsync(trainer.Id, "Welcome to HealthTrack",
                            "Your trainer account was created by the administrator. Please change your temporary password under your profile.",
                            NotificationType.System, "/Identity/Account/Manage/ChangePassword");

                        TempData["Success"] = $"Trainer account for {trainer.FullName} created. They can now log in with the temporary password.";
                        return RedirectToAction(nameof(Users), new { role = AppRoles.Trainer });
                    }

                    // Never leave a user without a role
                    await _users.DeleteAsync(trainer);
                    AddIdentityErrors(roleResult);
                }
                else
                {
                    AddIdentityErrors(result);   // e.g. email already taken, weak password
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Creating trainer {Email} failed", input.Email);
                ModelState.AddModelError(string.Empty, "Something went wrong while creating the account. Please try again.");
            }

            ViewBag.Specializations = new SelectList(Specializations, input.Specialization);
            return View(input);
        }

        // POST: Admin/ToggleActive/{id} - activate or deactivate an account
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user is null) return NotFound();

            // An admin must never lock themselves out
            if (user.Id == _users.GetUserId(User))
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Users));
            }

            try
            {
                user.IsActive = !user.IsActive;
                var result = await _users.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Users));
                }

                if (!user.IsActive)
                {
                    // Changing the security stamp signs the user out within 1 minute (see Program.cs)
                    await _users.UpdateSecurityStampAsync(user);

                    // A deactivated trainer can no longer look after clients
                    if (await _users.IsInRoleAsync(user, AppRoles.Trainer))
                    {
                        var trainerAssignments = await _assignments.FindAsync(a => a.TrainerId == user.Id && a.IsActive);
                        foreach (var assignment in trainerAssignments)
                        {
                            assignment.IsActive = false;
                            _assignments.Update(assignment);
                        }
                        await _assignments.SaveAsync();
                    }
                }

                _logger.LogInformation("Admin {Action} account {Email}", user.IsActive ? "activated" : "deactivated", user.Email);
                TempData["Success"] = user.IsActive
                    ? $"{user.FullName}'s account has been activated."
                    : $"{user.FullName}'s account has been deactivated. Their health records are kept.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Changing the status of user {UserId} failed", id);
                TempData["Error"] = "Sorry, the account status could not be changed. Please try again.";
            }

            return RedirectToAction(nameof(Users));
        }

        // ===============================================================
        // TRAINER ASSIGNMENTS
        // ===============================================================

        // GET: Admin/Assignments
        public async Task<IActionResult> Assignments()
        {
            return View(await BuildAssignmentsViewModelAsync(new AssignTrainerInput()));
        }

        // POST: Admin/Assign - assigns (or re-assigns) a client to a trainer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign([Bind(Prefix = "Input")] AssignTrainerInput input)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Assignments), await BuildAssignmentsViewModelAsync(input));
            }

            // Check both people really have the right role and are active
            var client = await _users.FindByIdAsync(input.ClientId);
            var trainer = await _users.FindByIdAsync(input.TrainerId);
            if (client is not { IsActive: true } || !await _users.IsInRoleAsync(client, AppRoles.Client))
            {
                ModelState.AddModelError("Input.ClientId", "Please choose a valid, active client.");
            }
            if (trainer is not { IsActive: true } || !await _users.IsInRoleAsync(trainer, AppRoles.Trainer))
            {
                ModelState.AddModelError("Input.TrainerId", "Please choose a valid, active trainer.");
            }
            if (!ModelState.IsValid)
            {
                return View(nameof(Assignments), await BuildAssignmentsViewModelAsync(input));
            }

            // One active trainer per client: end the old assignment but keep it as history
            var current = await _assignments.GetActiveForClientAsync(client!.Id);
            if (current != null)
            {
                if (current.TrainerId == trainer!.Id)
                {
                    TempData["Error"] = $"{client.FullName} is already assigned to {trainer.FullName}.";
                    return RedirectToAction(nameof(Assignments));
                }
                current.IsActive = false;
                _assignments.Update(current);
            }

            await _assignments.AddAsync(new TrainerAssignment
            {
                ClientId = client.Id,
                TrainerId = trainer!.Id,
                AssignedDate = DateTime.Now,
                IsActive = true
            });

            try
            {
                await _assignments.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database save failed in AdminController.Assign");
                TempData["Error"] = "Sorry, the assignment could not be saved. Please try again.";
                return RedirectToAction(nameof(Assignments));
            }

            await NotifySafelyAsync(client.Id, "New trainer assigned",
                $"{trainer.FullName} is now your trainer. You can book an appointment with them.",
                NotificationType.System, "/Appointments/Create");
            await NotifySafelyAsync(trainer.Id, "New client assigned",
                $"{client.FullName} has been assigned to you.",
                NotificationType.System, "/Trainer/Clients");

            TempData["Success"] = $"{client.FullName} is now assigned to {trainer.FullName}.";
            return RedirectToAction(nameof(Assignments));
        }

        // POST: Admin/Unassign/5 - ends an assignment (kept as history)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unassign(int id)
        {
            var assignment = await _assignments.GetByIdAsync(id);
            if (assignment is null || !assignment.IsActive) return NotFound();

            assignment.IsActive = false;
            _assignments.Update(assignment);
            try
            {
                await _assignments.SaveAsync();
                TempData["Success"] = "Assignment removed.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database save failed in AdminController.Unassign");
                TempData["Error"] = "Sorry, the assignment could not be removed. Please try again.";
            }
            return RedirectToAction(nameof(Assignments));
        }

        // ===============================================================
        // REPORTS
        // ===============================================================

        // GET: Admin/Reports
        public async Task<IActionResult> Reports()
        {
            DateTime since = DateTime.Today.AddDays(-29);   // last 30 days

            var clients = await _users.GetUsersInRoleAsync(AppRoles.Client);
            var trainers = await _users.GetUsersInRoleAsync(AppRoles.Trainer);
            var activeAssignments = (await _assignments.FindAsync(a => a.IsActive)).ToList();
            var workouts = (await _workoutLogs.GetAllAsync()).ToList();
            var meals = (await _mealLogs.GetAllAsync()).ToList();
            var measurements = (await _measurements.GetAllAsync()).ToList();
            var workoutPlans = (await _workoutPlans.GetAllAsync()).ToList();
            var dietPlans = (await _dietPlans.GetAllAsync()).ToList();
            var appointments = (await _appointments.GetAllAsync()).ToList();
            var feedback = (await _feedback.GetAllAsync()).ToList();

            // ---- 1. Client engagement (activity in the last 30 days) ----
            var engagement = new List<ClientEngagementRow>();
            foreach (var client in clients.Where(c => c.IsActive))
            {
                var clientWorkouts = workouts.Where(w => w.ClientId == client.Id).ToList();
                var clientMeals = meals.Where(m => m.ClientId == client.Id).ToList();
                var clientMeasurements = measurements.Where(h => h.ClientId == client.Id).ToList();

                int recentWorkouts = clientWorkouts.Count(w => w.Date >= since);
                int recentMeals = clientMeals.Count(m => m.Date >= since);
                int recentMeasurements = clientMeasurements.Count(h => h.Date >= since);

                var trainerId = activeAssignments.FirstOrDefault(a => a.ClientId == client.Id)?.TrainerId;
                var allDates = clientWorkouts.Select(w => w.Date)
                    .Concat(clientMeals.Select(m => m.Date))
                    .Concat(clientMeasurements.Select(h => h.Date))
                    .ToList();

                engagement.Add(new ClientEngagementRow
                {
                    Name = client.FullName,
                    TrainerName = trainers.FirstOrDefault(t => t.Id == trainerId)?.FullName ?? "Not assigned",
                    WorkoutsLast30Days = recentWorkouts,
                    MealsLast30Days = recentMeals,
                    MeasurementsLast30Days = recentMeasurements,
                    CaloriesBurnedLast30Days = clientWorkouts.Where(w => w.Date >= since).Sum(w => w.CaloriesBurned),
                    LastActivity = allDates.Count > 0 ? allDates.Max() : null,
                    EngagementLevel = GetEngagementLevel(recentWorkouts + recentMeals + recentMeasurements)
                });
            }

            // ---- 2. Popular routines ----
            var popularActivities = workouts
                .GroupBy(w => w.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new PopularActivityRow { Activity = g.Key, Sessions = g.Count() })
                .OrderByDescending(x => x.Sessions).ThenBy(x => x.Activity)
                .Take(5).ToList();

            var popularPlans = workoutPlans
                .GroupBy(p => p.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new PopularActivityRow { Activity = g.Key, Sessions = g.Count() })
                .OrderByDescending(x => x.Sessions).ThenBy(x => x.Activity)
                .Take(5).ToList();

            // ---- 3. Trainer performance ----
            var performance = trainers
                .Select(t =>
                {
                    var ratings = feedback.Where(f => f.TrainerId == t.Id && f.Rating.HasValue)
                                          .Select(f => f.Rating!.Value).ToList();
                    return new TrainerPerformanceRow
                    {
                        Name = t.FullName,
                        Specialization = t.Specialization,
                        Clients = activeAssignments.Count(a => a.TrainerId == t.Id),
                        WorkoutPlans = workoutPlans.Count(p => p.TrainerId == t.Id),
                        DietPlans = dietPlans.Count(p => p.TrainerId == t.Id),
                        AppointmentsCompleted = appointments.Count(a => a.TrainerId == t.Id && a.Status == AppointmentStatus.Completed),
                        FeedbackGiven = feedback.Count(f => f.TrainerId == t.Id),
                        AverageRating = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : null
                    };
                })
                .OrderByDescending(r => r.Clients).ThenBy(r => r.Name)
                .ToList();

            return View(new AdminReportViewModel
            {
                GeneratedAt = DateTime.Now,
                ClientEngagement = engagement.OrderBy(e => e.Name).ToList(),
                PopularActivities = popularActivities,
                PopularWorkoutPlans = popularPlans,
                TrainerPerformance = performance,
                HighEngagement = engagement.Count(e => e.EngagementLevel == "High"),
                MediumEngagement = engagement.Count(e => e.EngagementLevel == "Medium"),
                LowEngagement = engagement.Count(e => e.EngagementLevel == "Low"),
                InactiveClients = engagement.Count(e => e.EngagementLevel == "Inactive")
            });
        }

        // ===============================================================
        // Helper methods
        // ===============================================================

        /// <summary>
        /// Engagement rule based on the number of logs in the last 30 days:
        /// 20+ = High, 8-19 = Medium, 1-7 = Low, 0 = Inactive.
        /// </summary>
        private static string GetEngagementLevel(int logsInLast30Days)
        {
            if (logsInLast30Days >= 20) return "High";
            if (logsInLast30Days >= 8) return "Medium";
            if (logsInLast30Days >= 1) return "Low";
            return "Inactive";
        }

        /// <summary>Turns users into table rows, including each user's role.</summary>
        private async Task<List<AdminUserRow>> BuildUserRowsAsync(IEnumerable<ApplicationUser> users)
        {
            // 3 queries (one per role) instead of one query per user
            var adminIds = (await _users.GetUsersInRoleAsync(AppRoles.Admin)).Select(u => u.Id).ToHashSet();
            var trainerIds = (await _users.GetUsersInRoleAsync(AppRoles.Trainer)).Select(u => u.Id).ToHashSet();
            var clientIds = (await _users.GetUsersInRoleAsync(AppRoles.Client)).Select(u => u.Id).ToHashSet();

            return users.Select(u => new AdminUserRow
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = adminIds.Contains(u.Id) ? AppRoles.Admin
                     : trainerIds.Contains(u.Id) ? AppRoles.Trainer
                     : clientIds.Contains(u.Id) ? AppRoles.Client
                     : "No role",
                Specialization = u.Specialization,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();
        }

        /// <summary>Loads everything the Assignments page needs.</summary>
        private async Task<AssignmentsViewModel> BuildAssignmentsViewModelAsync(AssignTrainerInput input)
        {
            var allAssignments = (await _assignments.GetAllWithUsersAsync()).ToList();
            var active = allAssignments.Where(a => a.IsActive).OrderBy(a => a.Trainer?.FirstName).ThenBy(a => a.Client?.FirstName).ToList();

            var clients = (await _users.GetUsersInRoleAsync(AppRoles.Client)).Where(c => c.IsActive)
                .OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ToList();
            var trainers = (await _users.GetUsersInRoleAsync(AppRoles.Trainer)).Where(t => t.IsActive)
                .OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToList();

            var assignedIds = active.Select(a => a.ClientId).ToHashSet();

            return new AssignmentsViewModel
            {
                ActiveAssignments = active,
                HistoryCount = allAssignments.Count - active.Count,
                UnassignedClientNames = clients.Where(c => !assignedIds.Contains(c.Id)).Select(c => c.FullName).ToList(),
                Clients = clients.Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = assignedIds.Contains(c.Id) ? $"{c.FullName} (re-assign)" : c.FullName
                }).ToList(),
                Trainers = trainers.Select(t => new SelectListItem
                {
                    Value = t.Id,
                    Text = string.IsNullOrEmpty(t.Specialization) ? t.FullName : $"{t.FullName} - {t.Specialization}"
                }).ToList(),
                Input = input
            };
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        /// <summary>Sends a notification; a notification problem never breaks the admin action.</summary>
        private async Task NotifySafelyAsync(string userId, string title, string message, NotificationType type, string? link)
        {
            try
            {
                await _notificationService.SendAsync(userId, title, message, type, link);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send notification to {UserId}", userId);
            }
        }
    }
}
