using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    /// <summary>
    /// Task 8 - Notifications. Every signed-in user (Client, Trainer, Admin) can see,
    /// open, mark as read and delete ONLY their own notifications.
    /// </summary>
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationRepository _notifications;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationRepository notifications,
            INotificationService notificationService,
            UserManager<ApplicationUser> users,
            ILogger<NotificationsController> logger)
        {
            _notifications = notifications;
            _notificationService = notificationService;
            _users = users;
            _logger = logger;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var userId = _users.GetUserId(User);
            if (userId is null) return Challenge();

            // Make sure today's reminders exist before showing the list
            try
            {
                await _notificationService.GenerateRemindersAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not generate reminders in NotificationsController.Index");
            }

            var list = await _notifications.GetForUserAsync(userId);   // newest first
            return View(list.ToList());
        }

        // POST: Notifications/Open/5 - marks it as read and opens the related page
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(int id)
        {
            var notification = await GetOwnNotificationAsync(id);
            if (notification is null) return NotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _notifications.Update(notification);
                await SaveSafelyAsync("Open");
            }

            // Only follow links inside HealthTrack (prevents open-redirect attacks)
            if (!string.IsNullOrEmpty(notification.Link) && Url.IsLocalUrl(notification.Link))
            {
                return LocalRedirect(notification.Link);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Notifications/MarkAsRead/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await GetOwnNotificationAsync(id);
            if (notification is null) return NotFound();

            notification.IsRead = true;
            _notifications.Update(notification);
            await SaveSafelyAsync("MarkAsRead");
            return RedirectToAction(nameof(Index));
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _users.GetUserId(User);
            if (userId is null) return Challenge();

            try
            {
                await _notifications.MarkAllAsReadAsync(userId);
                TempData["Success"] = "All notifications marked as read.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database save failed in NotificationsController.MarkAllAsRead");
                TempData["Error"] = "Sorry, the notifications could not be updated. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Notifications/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await GetOwnNotificationAsync(id);
            if (notification is null) return NotFound();

            _notifications.Delete(notification);
            if (await SaveSafelyAsync("Delete"))
            {
                TempData["Success"] = "Notification deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Loads a notification only if it belongs to the signed-in user.
        /// Returns null otherwise, so users can never read or change other people's notifications.
        /// </summary>
        private async Task<Notification?> GetOwnNotificationAsync(int id)
        {
            var userId = _users.GetUserId(User);
            if (userId is null) return null;

            var notification = await _notifications.GetByIdAsync(id);
            return notification is not null && notification.UserId == userId ? notification : null;
        }

        /// <summary>Saves changes; logs the error and shows a friendly message if it fails.</summary>
        private async Task<bool> SaveSafelyAsync(string action)
        {
            try
            {
                await _notifications.SaveAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database save failed in NotificationsController.{Action}", action);
                TempData["Error"] = "Sorry, the notification could not be updated. Please try again.";
                return false;
            }
        }
    }
}
