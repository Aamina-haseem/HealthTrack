// =====================================================================
// FILE:    Controllers/AppointmentsController.cs
// PURPOSE: The appointment system (Task 6).
//            Clients:  book a consultation with a trainer, see and cancel their own.
//            Trainers: approve, reschedule (with private notes), complete or cancel.
//          Every change sends a notification to the other person.
// SECURITY: role-based [Authorize]; IsParticipant() makes sure only the client
//           and the trainer of an appointment can see or change it (others get
//           404); the client id comes from the signed-in user; a chosen trainer
//           must be a real, active trainer; all changes are POST + anti-forgery.
// BUSINESS RULES: opening hours / 1 hour ahead / 90 days max (AppointmentRules),
//           no double-booking within an hour (HasClashAsync), and cancelled or
//           completed appointments are final (CanBeChanged).
// ERRORS:  saves go through SaveSafelyAsync (try/catch DbUpdateException) and
//          notifications through NotifySafelyAsync, so a notification problem
//          never breaks the appointment itself.
// VIEWS:   Views/Appointments/*.cshtml
// =====================================================================

using HealthTrack.Models;                  // Appointment, AppointmentStatus, NotificationType, AppRoles
using HealthTrack.Repositories.Interfaces; // IAppointmentRepository, ITrainerAssignmentRepository
using HealthTrack.Services;                // INotificationService
using HealthTrack.ViewModels;              // the appointment view models
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult, [HttpPost]
using Microsoft.AspNetCore.Mvc.Rendering;  // SelectListItem (trainer drop-down)
using Microsoft.EntityFrameworkCore;       // DbUpdateException

namespace HealthTrack.Controllers
{
    /// <summary>
    /// Task 6 - Appointment System.
    ///  Clients:  book consultations with a trainer / health professional, view and cancel them.
    ///  Trainers: approve, reschedule (update), complete or cancel their appointments.
    /// Every action checks that the appointment belongs to the signed-in user.
    /// </summary>
    [Authorize(Roles = AppRoles.Client + "," + AppRoles.Trainer)]          // clients and trainers (some actions narrow it)
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentRepository _appointments;              // appointments table
        private readonly ITrainerAssignmentRepository _assignments;         // to pre-select the client's trainer
        private readonly INotificationService _notificationService;         // sends notifications
        private readonly UserManager<ApplicationUser> _users;               // users and roles
        private readonly ILogger<AppointmentsController> _logger;           // writes errors to the log

        // Constructor: ASP.NET Core passes in every dependency
        public AppointmentsController(
            IAppointmentRepository appointments,
            ITrainerAssignmentRepository assignments,
            INotificationService notificationService,
            UserManager<ApplicationUser> users,
            ILogger<AppointmentsController> logger)
        {
            _appointments = appointments;            // keep each dependency in a field
            _assignments = assignments;
            _notificationService = notificationService;
            _users = users;
            _logger = logger;
        }

        // GET: Appointments?status=Pending
        public async Task<IActionResult> Index(string? status)
        {
            var user = await GetActiveUserAsync();                         // null if deactivated
            if (user is null) return Forbid();

            bool isTrainer = User.IsInRole(AppRoles.Trainer);              // trainers and clients see different lists
            var all = (isTrainer
                ? await _appointments.GetByTrainerAsync(user.Id)           // the trainer's appointments
                : await _appointments.GetByClientAsync(user.Id)).ToList(); // the client's appointments

            // Optional filter by status (Pending, Approved, Cancelled, Completed)
            var shown = all;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, out var statusFilter))   // a valid status?
            {
                shown = all.Where(a => a.Status == statusFilter).ToList();    // keep only that status
            }

            return View(new AppointmentListViewModel
            {
                IsTrainer = isTrainer,
                StatusFilter = status,                                     // highlights the selected tab
                PendingCount = all.Count(a => a.Status == AppointmentStatus.Pending),   // number on the Pending tab
                Appointments = shown
            });
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await GetActiveUserAsync();
            if (user is null) return Forbid();

            var appointment = await _appointments.GetWithDetailsAsync(id);    // with client and trainer loaded
            if (appointment is null || !IsParticipant(appointment, user.Id)) return NotFound();   // not theirs: 404

            ViewBag.IsTrainer = appointment.TrainerId == user.Id;          // shows the trainer's buttons
            return View(appointment);
        }

        // GET: Appointments/Create  (clients only)
        [Authorize(Roles = AppRoles.Client)]
        public async Task<IActionResult> Create()
        {
            var user = await GetActiveUserAsync();
            if (user is null) return Forbid();

            // Suggest tomorrow at 10:00 with the client's own trainer pre-selected
            var assignment = await _assignments.GetActiveForClientAsync(user.Id);
            var model = new AppointmentCreateViewModel
            {
                TrainerId = assignment?.TrainerId ?? string.Empty,         // their trainer, if they have one
                AppointmentDate = DateTime.Today.AddDays(1).AddHours(10)   // tomorrow 10:00
            };
            model.Trainers = await GetTrainerOptionsAsync();               // fill the drop-down
            return View(model);
        }

        // POST: Appointments/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Client)]
        public async Task<IActionResult> Create(AppointmentCreateViewModel input)
        {
            var client = await GetActiveUserAsync();
            if (client is null) return Forbid();

            // The trainer must be a real, active trainer (never trust the drop-down value)
            var trainer = string.IsNullOrEmpty(input.TrainerId) ? null : await _users.FindByIdAsync(input.TrainerId);
            if (trainer is null || !trainer.IsActive || !await _users.IsInRoleAsync(trainer, AppRoles.Trainer))
            {
                ModelState.AddModelError(nameof(input.TrainerId), "Please choose a valid trainer.");
            }
            else if (await HasClashAsync(trainer.Id, input.AppointmentDate, excludeId: null))   // double booking?
            {
                ModelState.AddModelError(nameof(input.AppointmentDate),
                    "This trainer already has an appointment within an hour of that time. Please choose another time.");
            }

            if (!ModelState.IsValid)                                       // includes the AppointmentRules checks
            {
                input.Trainers = await GetTrainerOptionsAsync();
                return View(input);                                        // show the form again with errors
            }

            var appointment = new Appointment
            {
                ClientId = client.Id,               // always the signed-in user, never from the form
                TrainerId = trainer!.Id,
                AppointmentDate = input.AppointmentDate,
                Reason = input.Reason.Trim(),
                Status = AppointmentStatus.Pending,  // the trainer must approve it
                CreatedAt = DateTime.Now
            };
            await _appointments.AddAsync(appointment);                     // mark it to be inserted

            if (!await SaveSafelyAsync("Create"))                          // save failed?
            {
                input.Trainers = await GetTrainerOptionsAsync();
                return View(input);
            }

            // Tell the trainer about the new request
            await NotifySafelyAsync(trainer.Id, "New appointment request",
                $"{client.FullName} requested an appointment on {appointment.AppointmentDate:ddd dd MMM 'at' HH:mm}.",
                appointment.Id);

            TempData["Success"] = "Appointment requested. You will be notified when the trainer approves it.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Approve/5  (trainers only)
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Trainer)]
        public async Task<IActionResult> Approve(int id)
        {
            var trainer = await GetActiveUserAsync();
            if (trainer is null) return Forbid();

            // Loaded without related users, so Update() only touches the Appointments table
            var appointment = await _appointments.GetByIdAsync(id);
            if (appointment is null || appointment.TrainerId != trainer.Id) return NotFound();   // only their own

            if (appointment.Status != AppointmentStatus.Pending)           // only pending ones can be approved
            {
                TempData["Error"] = "Only pending appointments can be approved.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (appointment.AppointmentDate < DateTime.Now)                // the time has already gone
            {
                TempData["Error"] = "This appointment time has already passed. Please reschedule it instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            appointment.Status = AppointmentStatus.Approved;               // approve it
            _appointments.Update(appointment);
            if (await SaveSafelyAsync("Approve"))
            {
                // Tell the client
                await NotifySafelyAsync(appointment.ClientId, "Appointment approved",
                    $"{trainer.FullName} approved your appointment on {appointment.AppointmentDate:ddd dd MMM 'at' HH:mm}.",
                    appointment.Id);
                TempData["Success"] = "Appointment approved. The client has been notified.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Reschedule/5  (trainers only)
        [Authorize(Roles = AppRoles.Trainer)]
        public async Task<IActionResult> Reschedule(int id)
        {
            var trainer = await GetActiveUserAsync();
            if (trainer is null) return Forbid();

            var appointment = await _appointments.GetWithDetailsAsync(id);
            if (appointment is null || appointment.TrainerId != trainer.Id) return NotFound();   // only their own
            if (!CanBeChanged(appointment))                                // cancelled/completed are final
            {
                TempData["Error"] = "Cancelled or completed appointments cannot be changed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(new AppointmentRescheduleViewModel
            {
                Id = appointment.Id,
                ClientName = appointment.Client?.FullName ?? "Client",
                Reason = appointment.Reason,
                AppointmentDate = appointment.AppointmentDate,
                TrainerNotes = appointment.TrainerNotes                    // decrypted automatically when loaded
            });
        }

        // POST: Appointments/Reschedule/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Trainer)]
        public async Task<IActionResult> Reschedule(int id, AppointmentRescheduleViewModel input)
        {
            var trainer = await GetActiveUserAsync();
            if (trainer is null) return Forbid();

            var appointment = await _appointments.GetByIdAsync(id);
            if (appointment is null || appointment.TrainerId != trainer.Id) return NotFound();   // check ownership again
            if (!CanBeChanged(appointment))
            {
                TempData["Error"] = "Cancelled or completed appointments cannot be changed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (ModelState.IsValid && await HasClashAsync(trainer.Id, input.AppointmentDate, excludeId: id))   // clash with another one?
            {
                ModelState.AddModelError(nameof(input.AppointmentDate),
                    "You already have another appointment within an hour of that time.");
            }
            if (!ModelState.IsValid)
            {
                var client = await _users.FindByIdAsync(appointment.ClientId);
                input.ClientName = client?.FullName ?? "Client";            // refill the display-only fields
                input.Reason = appointment.Reason;
                return View(input);
            }

            bool timeChanged = appointment.AppointmentDate != input.AppointmentDate;   // decides the notification text
            appointment.AppointmentDate = input.AppointmentDate;
            appointment.TrainerNotes = string.IsNullOrWhiteSpace(input.TrainerNotes) ? null : input.TrainerNotes.Trim();   // encrypted on save
            appointment.Status = AppointmentStatus.Approved;   // the trainer set the time, so it is confirmed
            _appointments.Update(appointment);

            if (await SaveSafelyAsync("Reschedule"))
            {
                string message = timeChanged
                    ? $"{trainer.FullName} moved your appointment to {appointment.AppointmentDate:ddd dd MMM 'at' HH:mm}."
                    : $"{trainer.FullName} confirmed your appointment on {appointment.AppointmentDate:ddd dd MMM 'at' HH:mm}.";
                await NotifySafelyAsync(appointment.ClientId, "Appointment updated", message, appointment.Id);   // tell the client
                TempData["Success"] = "Appointment updated. The client has been notified.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Appointments/Complete/5  (trainers only, after the appointment took place)
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Trainer)]
        public async Task<IActionResult> Complete(int id)
        {
            var trainer = await GetActiveUserAsync();
            if (trainer is null) return Forbid();

            var appointment = await _appointments.GetByIdAsync(id);
            if (appointment is null || appointment.TrainerId != trainer.Id) return NotFound();   // only their own

            if (appointment.Status != AppointmentStatus.Approved || appointment.AppointmentDate > DateTime.Now)   // must be approved and in the past
            {
                TempData["Error"] = "Only approved appointments whose time has passed can be marked as completed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            appointment.Status = AppointmentStatus.Completed;              // mark it done
            _appointments.Update(appointment);
            if (await SaveSafelyAsync("Complete"))
            {
                TempData["Success"] = "Appointment marked as completed.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Cancel/5  (client or trainer of this appointment)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await GetActiveUserAsync();
            if (user is null) return Forbid();

            var appointment = await _appointments.GetByIdAsync(id);
            if (appointment is null || !IsParticipant(appointment, user.Id)) return NotFound();   // only the two participants

            if (!CanBeChanged(appointment))                                // already cancelled or completed
            {
                TempData["Error"] = "This appointment is already cancelled or completed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            appointment.Status = AppointmentStatus.Cancelled;              // cancel it
            _appointments.Update(appointment);

            if (await SaveSafelyAsync("Cancel"))
            {
                // Tell the OTHER person about the cancellation
                bool cancelledByTrainer = appointment.TrainerId == user.Id;
                string otherUserId = cancelledByTrainer ? appointment.ClientId : appointment.TrainerId;
                await NotifySafelyAsync(otherUserId, "Appointment cancelled",
                    $"{user.FullName} cancelled the appointment on {appointment.AppointmentDate:ddd dd MMM 'at' HH:mm}.",
                    appointment.Id);
                TempData["Success"] = "Appointment cancelled.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------------------
        // Helper methods
        // ---------------------------------------------------------------

        /// <summary>The signed-in user, or null if their account has been deactivated.</summary>
        private async Task<ApplicationUser?> GetActiveUserAsync()
        {
            var user = await _users.GetUserAsync(User);                    // load the signed-in user
            return user is { IsActive: true } ? user : null;               // deactivated = null
        }

        /// <summary>Ownership check: only the client and the trainer of an appointment may see or change it.</summary>
        private static bool IsParticipant(Appointment appointment, string userId)
        {
            return appointment.ClientId == userId || appointment.TrainerId == userId;
        }

        /// <summary>Cancelled and completed appointments are final.</summary>
        private static bool CanBeChanged(Appointment appointment)
        {
            return appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Approved;
        }

        /// <summary>
        /// Double-booking check: true if the trainer already has a pending/approved
        /// appointment less than one hour before or after the requested time.
        /// </summary>
        private async Task<bool> HasClashAsync(string trainerId, DateTime requested, int? excludeId)
        {
            DateTime from = requested.AddHours(-1);                        // one hour before
            DateTime to = requested.AddHours(1);                           // one hour after
            var clashes = await _appointments.FindAsync(a =>
                a.TrainerId == trainerId &&                                // same trainer
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved) &&   // still going ahead
                a.AppointmentDate > from && a.AppointmentDate < to &&      // inside the window
                (excludeId == null || a.Id != excludeId));                 // ignore the one being rescheduled
            return clashes.Any();
        }

        /// <summary>All active trainers for the drop-down, e.g. "Kamal Perera - Fitness Trainer".</summary>
        private async Task<IEnumerable<SelectListItem>> GetTrainerOptionsAsync()
        {
            var trainers = await _users.GetUsersInRoleAsync(AppRoles.Trainer);   // everyone with the Trainer role
            return trainers
                .Where(t => t.IsActive)                                    // active ones only
                .OrderBy(t => t.FirstName).ThenBy(t => t.LastName)         // alphabetical
                .Select(t => new SelectListItem
                {
                    Value = t.Id,                                          // what the form posts
                    Text = string.IsNullOrEmpty(t.Specialization) ? t.FullName : $"{t.FullName} - {t.Specialization}"   // what the client sees
                })
                .ToList();
        }

        /// <summary>Saves changes; logs the error and shows a friendly message if it fails.</summary>
        private async Task<bool> SaveSafelyAsync(string action)
        {
            try
            {
                await _appointments.SaveAsync();                           // write the changes
                return true;                                               // success
            }
            catch (DbUpdateException ex)                                   // the database save failed
            {
                // Technical details go to the log; the user only sees a simple message
                _logger.LogError(ex, "Database save failed in AppointmentsController.{Action}", action);
                TempData["Error"] = "Sorry, the appointment could not be saved. Please try again.";
                ModelState.AddModelError(string.Empty, "Sorry, the appointment could not be saved. Please try again.");
                return false;                                              // tell the caller it failed
            }
        }

        /// <summary>Sends a notification; a notification problem never breaks the appointment action.</summary>
        private async Task NotifySafelyAsync(string userId, string title, string message, int appointmentId)
        {
            try
            {
                await _notificationService.SendAsync(userId, title, message,
                    NotificationType.Appointment, $"/Appointments/Details/{appointmentId}");   // links to the appointment
            }
            catch (Exception ex)                                           // any problem: log it and carry on
            {
                _logger.LogError(ex, "Could not send appointment notification to {UserId}", userId);
            }
        }
    }
}
