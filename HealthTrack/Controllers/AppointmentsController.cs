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
    /// Task 6 - Appointment System.
    ///  Clients:  book consultations with a trainer / health professional, view and cancel them.
    ///  Trainers: approve, reschedule (update), complete or cancel their appointments.
    /// Every action checks that the appointment belongs to the signed-in user.
    /// </summary>
    [Authorize(Roles = AppRoles.Client + "," + AppRoles.Trainer)]
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentRepository _appointments;
        private readonly ITrainerAssignmentRepository _assignments;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            IAppointmentRepository appointments,
            ITrainerAssignmentRepository assignments,
            INotificationService notificationService,
            UserManager<ApplicationUser> users,
            ILogger<AppointmentsController> logger)
        {
            _appointments = appointments;
            _assignments = assignments;
            _notificationService = notificationService;
            _users = users;
            _logger = logger;
        }

        // GET: Appointments?status=Pending
        public async Task<IActionResult> Index(string? status)
        {
            var user = await GetActiveUserAsync();
            if (user is null) return Forbid();

            bool isTrainer = User.IsInRole(AppRoles.Trainer);
            var all = (isTrainer
                ? await _appointments.GetByTrainerAsync(user.Id)
                : await _appointments.GetByClientAsync(user.Id)).ToList();

            // Optional filter by status (Pending, Approved, Cancelled, Completed)
            var shown = all;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, out var statusFilter))
            {
                shown = all.Where(a => a.Status == statusFilter).ToList();
            }

            return View(new AppointmentListViewModel
            {
                IsTrainer = isTrainer,
                StatusFilter = status,
                PendingCount = all.Count(a => a.Status == AppointmentStatus.Pending),
                Appointments = shown
            });
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await GetActiveUserAsync();
            if (user is null) return Forbid();

            var appointment = await _appointments.GetWithDetailsAsync(id);
            if (appointment is null || !IsParticipant(appointment, user.Id)) return NotFound();

            ViewBag.IsTrainer = appointment.TrainerId == user.Id;
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
                TrainerId = assignment?.TrainerId ?? string.Empty,
                AppointmentDate = DateTime.Today.AddDays(1).AddHours(10)
            };
            model.Trainers = await GetTrainerOptionsAsync();
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
            else if (await HasClashAsync(trainer.Id, input.AppointmentDate, excludeId: null))
            {
                ModelState.AddModelError(nameof(input.AppointmentDate),
                    "This trainer already has an appointment within an hour of that time. Please choose another time.");
            }

            if (!ModelState.IsValid)
            {
                input.Trainers = await GetTrainerOptionsAsync();
                return View(input);
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
            await _appointments.AddAsync(appointment);

            if (!await SaveSafelyAsync("Create"))
            {
                input.Trainers = await GetTrainerOptionsAsync();
                return View(input);
            }

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
            if (appointment is null || appointment.TrainerId != trainer.Id) return NotFound();

            if (appointment.Status != AppointmentStatus.Pending)
            {
                TempData["Error"] = "Only pending appointments can be approved.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (appointment.AppointmentDate < DateTime.Now)
            {
                TempData["Error"] = "This appointment time has already passed. Please reschedule it instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            appointment.Status = AppointmentStatus.Approved;
            _appointments.Update(appointment);
            if (await SaveSafelyAsync("Approve"))
            {
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
            if (appointment is null || appointment.TrainerId != trainer.Id) return NotFound();
            if (!CanBeChanged(appointment))
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
                TrainerNotes = appointment.TrainerNotes
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
            if (appointment is null || appointment.TrainerId != trainer.Id) return NotFound();
            if (!CanBeChanged(appointment))
            {
                TempData["Error"] = "Cancelled or completed appointments cannot be changed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (ModelState.IsValid && await HasClashAsync(trainer.Id, input.AppointmentDate, excludeId: id))
            {
                ModelState.AddModelError(nameof(input.AppointmentDate),
                    "You already have another appointment within an hour of that time.");
            }
            if (!ModelState.IsValid)
            {
                var client = await _users.FindByIdAsync(appointment.ClientId);
                input.ClientName = client?.FullName ?? "Client";
                input.Reason = appointment.Reason;
                return View(input);
            }

            bool timeChanged = appointment.AppointmentDate != input.AppointmentDate;
            appointment.AppointmentDate = input.AppointmentDate;
            appointment.TrainerNotes = string.IsNullOrWhiteSpace(input.TrainerNotes) ? null : input.TrainerNotes.Trim();
            appointment.Status = AppointmentStatus.Approved;   // the trainer set the time, so it is confirmed
            _appointments.Update(appointment);

            if (await SaveSafelyAsync("Reschedule"))
            {
                string message = timeChanged
                    ? $"{trainer.FullName} moved your appointment to {appointment.AppointmentDate:ddd dd MMM 'at' HH:mm}."
                    : $"{trainer.FullName} confirmed your appointment on {appointment.AppointmentDate:ddd dd MMM 'at' HH:mm}.";
                await NotifySafelyAsync(appointment.ClientId, "Appointment updated", message, appointment.Id);
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
            if (appointment is null || appointment.TrainerId != trainer.Id) return NotFound();

            if (appointment.Status != AppointmentStatus.Approved || appointment.AppointmentDate > DateTime.Now)
            {
                TempData["Error"] = "Only approved appointments whose time has passed can be marked as completed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            appointment.Status = AppointmentStatus.Completed;
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
            if (appointment is null || !IsParticipant(appointment, user.Id)) return NotFound();

            if (!CanBeChanged(appointment))
            {
                TempData["Error"] = "This appointment is already cancelled or completed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            appointment.Status = AppointmentStatus.Cancelled;
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
            var user = await _users.GetUserAsync(User);
            return user is { IsActive: true } ? user : null;
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
            DateTime from = requested.AddHours(-1);
            DateTime to = requested.AddHours(1);
            var clashes = await _appointments.FindAsync(a =>
                a.TrainerId == trainerId &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved) &&
                a.AppointmentDate > from && a.AppointmentDate < to &&
                (excludeId == null || a.Id != excludeId));
            return clashes.Any();
        }

        /// <summary>All active trainers for the drop-down, e.g. "Kamal Perera - Fitness Trainer".</summary>
        private async Task<IEnumerable<SelectListItem>> GetTrainerOptionsAsync()
        {
            var trainers = await _users.GetUsersInRoleAsync(AppRoles.Trainer);
            return trainers
                .Where(t => t.IsActive)
                .OrderBy(t => t.FirstName).ThenBy(t => t.LastName)
                .Select(t => new SelectListItem
                {
                    Value = t.Id,
                    Text = string.IsNullOrEmpty(t.Specialization) ? t.FullName : $"{t.FullName} - {t.Specialization}"
                })
                .ToList();
        }

        /// <summary>Saves changes; logs the error and shows a friendly message if it fails.</summary>
        private async Task<bool> SaveSafelyAsync(string action)
        {
            try
            {
                await _appointments.SaveAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                // Technical details go to the log; the user only sees a simple message
                _logger.LogError(ex, "Database save failed in AppointmentsController.{Action}", action);
                TempData["Error"] = "Sorry, the appointment could not be saved. Please try again.";
                ModelState.AddModelError(string.Empty, "Sorry, the appointment could not be saved. Please try again.");
                return false;
            }
        }

        /// <summary>Sends a notification; a notification problem never breaks the appointment action.</summary>
        private async Task NotifySafelyAsync(string userId, string title, string message, int appointmentId)
        {
            try
            {
                await _notificationService.SendAsync(userId, title, message,
                    NotificationType.Appointment, $"/Appointments/Details/{appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send appointment notification to {UserId}", userId);
            }
        }
    }
}
