// =====================================================================
// FILE:    Controllers/AdminController.cs
// PURPOSE: The Administrator module (Task 4), for Admins only:
//            - Index:        the admin dashboard (users, activity, appointments, workload)
//            - Users:        search/filter users, activate or deactivate accounts
//            - CreateTrainer: create trainer / health professional accounts
//            - Assignments:  assign clients to trainers (one active trainer per client)
//            - Reports:      client engagement, popular routines, trainer performance
// SECURITY: [Authorize(Roles = Admin)]; all changes are POST + anti-forgery token;
//           an admin cannot deactivate themselves; deactivating a user changes their
//           security stamp so they are signed out within a minute (see Program.cs),
//           and a deactivated trainer loses their client assignments.
// DATA RULES: accounts are deactivated, never deleted, so health records are kept;
//             old assignments are kept as history (IsActive = false).
// VIEWS:   Views/Admin/*.cshtml
// =====================================================================

using HealthTrack.Models;                  // ApplicationUser, TrainerAssignment, logs, plans, AppRoles, ...
using HealthTrack.Repositories.Interfaces; // repository interfaces
using HealthTrack.Services;                // INotificationService
using HealthTrack.ViewModels;              // the admin view models
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager, IdentityResult
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult, [HttpPost], [Bind]
using Microsoft.AspNetCore.Mvc.Rendering;  // SelectList, SelectListItem
using Microsoft.EntityFrameworkCore;       // ToListAsync, DbUpdateException

namespace HealthTrack.Controllers
{
    /// <summary>
    /// Task 4 - Administrator module (Admins only).
    ///  Dashboard:   user activity, system usage and trainer assignments at a glance
    ///  Users:       search users, create Trainer accounts, activate / deactivate accounts
    ///  Assignments: assign clients to trainers (one active trainer per client)
    ///  Reports:     client engagement, popular routines and trainer performance
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]                                    // every action: admins only
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;              // users and roles
        private readonly ITrainerAssignmentRepository _assignments;        // trainer <-> client links
        private readonly IAppointmentRepository _appointments;             // appointments
        private readonly IRepository<WorkoutLog> _workoutLogs;             // workout logs
        private readonly IRepository<MealLog> _mealLogs;                   // meal logs
        private readonly IRepository<HealthMeasurement> _measurements;     // measurements
        private readonly IRepository<WorkoutPlan> _workoutPlans;           // workout plans
        private readonly IRepository<DietPlan> _dietPlans;                 // diet plans
        private readonly IRepository<Feedback> _feedback;                  // trainer feedback
        private readonly INotificationService _notificationService;        // sends notifications
        private readonly ILogger<AdminController> _logger;                 // writes to the log

        // Specializations offered when creating a trainer / health professional
        private static readonly string[] Specializations =
        {
            "Fitness Trainer", "Personal Trainer", "Nutritionist", "Dietitian",
            "Physiotherapist", "Yoga Instructor", "Doctor"
        };

        // Constructor: ASP.NET Core passes in every dependency
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
            _users = users;                          // keep each dependency in a field
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
            var admin = await _users.GetUserAsync(User);                   // the signed-in admin
            if (admin is null) return Challenge();                         // not signed in

            DateTime today = DateTime.Today;
            DateTime weekStart = today.AddDays(-6);       // last 7 days including today
            DateTime monthStart = today.AddDays(-29);     // last 30 days including today

            var clients = await _users.GetUsersInRoleAsync(AppRoles.Client);          // all clients
            var trainers = await _users.GetUsersInRoleAsync(AppRoles.Trainer);        // all trainers
            var allUsers = await _users.Users.ToListAsync();                          // every account
            var activeAssignments = (await _assignments.FindAsync(a => a.IsActive)).ToList();   // current links
            var appointments = (await _appointments.GetAllAsync()).ToList();          // every appointment

            // Logs from the last 7 days (for the KPI card and the activity chart)
            var workoutsThisWeek = (await _workoutLogs.FindAsync(w => w.Date >= weekStart)).ToList();
            var mealsThisWeek = (await _mealLogs.FindAsync(m => m.Date >= weekStart)).ToList();
            var measurementsThisWeek = (await _measurements.FindAsync(h => h.Date >= weekStart)).ToList();

            // Activity per day for the chart (oldest day first)
            var labels = new List<string>();                               // x axis: day names
            var counts = new List<int>();                                  // y axis: logs per day
            for (DateTime day = weekStart; day <= today; day = day.AddDays(1))   // each of the 7 days
            {
                labels.Add(day.ToString("ddd dd"));                        // e.g. "Mon 19"
                counts.Add(workoutsThisWeek.Count(w => w.Date.Date == day) // workouts that day
                         + mealsThisWeek.Count(m => m.Date.Date == day)    // + meals that day
                         + measurementsThisWeek.Count(h => h.Date.Date == day));   // + measurements that day
            }

            // Clients per trainer
            var workload = trainers
                .Select(t => new TrainerWorkloadRow
                {
                    TrainerId = t.Id,
                    Name = t.FullName,
                    Specialization = t.Specialization,
                    IsActive = t.IsActive,
                    ClientCount = activeAssignments.Count(a => a.TrainerId == t.Id),   // their active clients
                    UpcomingAppointments = appointments.Count(a => a.TrainerId == t.Id &&
                        a.AppointmentDate >= DateTime.Now &&
                        (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved))
                })
                .OrderByDescending(t => t.ClientCount).ThenBy(t => t.Name)    // busiest first
                .ToList();

            var assignedClientIds = activeAssignments.Select(a => a.ClientId).ToHashSet();   // fast "is assigned?" lookups

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
                    .OrderByDescending(u => u.CreatedAt).Take(5).ToList()  // 5 newest accounts
            };

            return View(model);
        }

        // ===============================================================
        // USERS
        // ===============================================================

        // GET: Admin/Users?role=Trainer&search=kamal
        public async Task<IActionResult> Users(string? role, string? search)
        {
            var allUsers = await _users.Users.ToListAsync();               // every account
            var rows = await BuildUserRowsAsync(allUsers);                 // turn them into table rows (with roles)

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
                Users = rows.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToList(),   // grouped by role, then name
                RoleFilter = role,                                         // keeps the filter selected
                Search = search,                                           // keeps the search text in the box
                CurrentAdminId = _users.GetUserId(User) ?? string.Empty    // "(you)" instead of a Deactivate button
            });
        }

        // GET: Admin/CreateTrainer
        public IActionResult CreateTrainer()
        {
            ViewBag.Specializations = new SelectList(Specializations);     // fill the drop-down
            return View(new CreateTrainerViewModel());                     // empty form
        }

        // POST: Admin/CreateTrainer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(CreateTrainerViewModel input)
        {
            if (!Specializations.Contains(input.Specialization))           // only values from our list are allowed
            {
                ModelState.AddModelError(nameof(input.Specialization), "Please choose a specialization from the list.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Specializations = new SelectList(Specializations, input.Specialization);
                return View(input);                                        // show the form again with errors
            }

            try
            {
                // Build the new account from the form
                var trainer = new ApplicationUser
                {
                    UserName = input.Email.Trim(),                         // signs in with the email
                    Email = input.Email.Trim(),
                    EmailConfirmed = true,                                 // no email server, so confirmed
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
                    var roleResult = await _users.AddToRoleAsync(trainer, AppRoles.Trainer);   // give the Trainer role
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("Admin created trainer account {Email}", trainer.Email);
                        // Welcome notification with a link to change the temporary password
                        await NotifySafelyAsync(trainer.Id, "Welcome to HealthTrack",
                            "Your trainer account was created by the administrator. Please change your temporary password under your profile.",
                            NotificationType.System, "/Identity/Account/Manage/ChangePassword");

                        TempData["Success"] = $"Trainer account for {trainer.FullName} created. They can now log in with the temporary password.";
                        return RedirectToAction(nameof(Users), new { role = AppRoles.Trainer });   // show the trainers list
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
            catch (Exception ex)                                           // anything unexpected
            {
                _logger.LogError(ex, "Creating trainer {Email} failed", input.Email);
                ModelState.AddModelError(string.Empty, "Something went wrong while creating the account. Please try again.");
            }

            ViewBag.Specializations = new SelectList(Specializations, input.Specialization);
            return View(input);                                            // show the form again with the errors
        }

        // POST: Admin/ToggleActive/{id} - activate or deactivate an account
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _users.FindByIdAsync(id);                     // the account to change
            if (user is null) return NotFound();

            // An admin must never lock themselves out
            if (user.Id == _users.GetUserId(User))
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Users));
            }

            try
            {
                user.IsActive = !user.IsActive;                            // flip active <-> inactive
                var result = await _users.UpdateAsync(user);               // save the account
                if (!result.Succeeded)
                {
                    TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Users));
                }

                if (!user.IsActive)                                        // the account was just deactivated
                {
                    // Changing the security stamp signs the user out within 1 minute (see Program.cs)
                    await _users.UpdateSecurityStampAsync(user);

                    // A deactivated trainer can no longer look after clients
                    if (await _users.IsInRoleAsync(user, AppRoles.Trainer))
                    {
                        var trainerAssignments = await _assignments.FindAsync(a => a.TrainerId == user.Id && a.IsActive);
                        foreach (var assignment in trainerAssignments)
                        {
                            assignment.IsActive = false;                   // end the assignment (kept as history)
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

            return RedirectToAction(nameof(Users));                        // back to the users list
        }

        // ===============================================================
        // TRAINER ASSIGNMENTS
        // ===============================================================

        // GET: Admin/Assignments
        public async Task<IActionResult> Assignments()
        {
            return View(await BuildAssignmentsViewModelAsync(new AssignTrainerInput()));   // empty form + lists
        }

        // POST: Admin/Assign - assigns (or re-assigns) a client to a trainer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign([Bind(Prefix = "Input")] AssignTrainerInput input)   // form fields are named Input.ClientId / Input.TrainerId
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
                if (current.TrainerId == trainer!.Id)                      // already with this trainer
                {
                    TempData["Error"] = $"{client.FullName} is already assigned to {trainer.FullName}.";
                    return RedirectToAction(nameof(Assignments));
                }
                current.IsActive = false;                                  // end the old link
                _assignments.Update(current);
            }

            await _assignments.AddAsync(new TrainerAssignment                // the new link
            {
                ClientId = client.Id,
                TrainerId = trainer!.Id,
                AssignedDate = DateTime.Now,
                IsActive = true
            });

            try
            {
                await _assignments.SaveAsync();                            // one save for both changes
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database save failed in AdminController.Assign");
                TempData["Error"] = "Sorry, the assignment could not be saved. Please try again.";
                return RedirectToAction(nameof(Assignments));
            }

            // Tell both people about the new assignment
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
            if (assignment is null || !assignment.IsActive) return NotFound();   // missing or already ended

            assignment.IsActive = false;                                   // end it (not deleted)
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

            // Load everything the report needs once, then work in memory
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
            foreach (var client in clients.Where(c => c.IsActive))         // active clients only
            {
                // This client's records
                var clientWorkouts = workouts.Where(w => w.ClientId == client.Id).ToList();
                var clientMeals = meals.Where(m => m.ClientId == client.Id).ToList();
                var clientMeasurements = measurements.Where(h => h.ClientId == client.Id).ToList();

                // How many in the last 30 days
                int recentWorkouts = clientWorkouts.Count(w => w.Date >= since);
                int recentMeals = clientMeals.Count(m => m.Date >= since);
                int recentMeasurements = clientMeasurements.Count(h => h.Date >= since);

                var trainerId = activeAssignments.FirstOrDefault(a => a.ClientId == client.Id)?.TrainerId;   // their trainer (or null)
                var allDates = clientWorkouts.Select(w => w.Date)            // every date they logged anything
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
                .GroupBy(w => w.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)   // "running" = "Running"
                .Select(g => new PopularActivityRow { Activity = g.Key, Sessions = g.Count() })
                .OrderByDescending(x => x.Sessions).ThenBy(x => x.Activity)
                .Take(5).ToList();                                         // top 5

            var popularPlans = workoutPlans
                .GroupBy(p => p.Title.Trim(), StringComparer.OrdinalIgnoreCase)          // plans with the same title
                .Select(g => new PopularActivityRow { Activity = g.Key, Sessions = g.Count() })   // how many clients have it
                .OrderByDescending(x => x.Sessions).ThenBy(x => x.Activity)
                .Take(5).ToList();

            // ---- 3. Trainer performance ----
            var performance = trainers
                .Select(t =>
                {
                    // The ratings this trainer has given
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
                        AverageRating = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : null   // null if no ratings
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
                // Counts for the engagement donut
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
                Role = adminIds.Contains(u.Id) ? AppRoles.Admin            // work out the role from the three sets
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
            var allAssignments = (await _assignments.GetAllWithUsersAsync()).ToList();     // active + history, with names
            var active = allAssignments.Where(a => a.IsActive).OrderBy(a => a.Trainer?.FirstName).ThenBy(a => a.Client?.FirstName).ToList();

            // Active clients and trainers for the two drop-downs, alphabetical
            var clients = (await _users.GetUsersInRoleAsync(AppRoles.Client)).Where(c => c.IsActive)
                .OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ToList();
            var trainers = (await _users.GetUsersInRoleAsync(AppRoles.Trainer)).Where(t => t.IsActive)
                .OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToList();

            var assignedIds = active.Select(a => a.ClientId).ToHashSet();   // clients who already have a trainer

            return new AssignmentsViewModel
            {
                ActiveAssignments = active,
                HistoryCount = allAssignments.Count - active.Count,        // ended assignments
                UnassignedClientNames = clients.Where(c => !assignedIds.Contains(c.Id)).Select(c => c.FullName).ToList(),
                Clients = clients.Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = assignedIds.Contains(c.Id) ? $"{c.FullName} (re-assign)" : c.FullName   // hint when they already have one
                }).ToList(),
                Trainers = trainers.Select(t => new SelectListItem
                {
                    Value = t.Id,
                    Text = string.IsNullOrEmpty(t.Specialization) ? t.FullName : $"{t.FullName} - {t.Specialization}"
                }).ToList(),
                Input = input                                              // keeps what the admin selected
            };
        }

        // Shows Identity's error messages (e.g. "Email is already taken") on the form
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
            catch (Exception ex)                                           // log it and carry on
            {
                _logger.LogError(ex, "Could not send notification to {UserId}", userId);
            }
        }
    }
}
