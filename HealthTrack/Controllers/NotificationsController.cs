// =====================================================================
// FILE:    Controllers/NotificationsController.cs
// PURPOSE: The Notifications page (Task 8). Any signed-in user can list, open,
//          mark as read and delete ONLY their own notifications.
// SECURITY: [Authorize] for every action; GetOwnNotificationAsync checks the
//           owner of every notification; links are only followed inside the site
//           (stops open-redirect attacks); all changes are POST + anti-forgery token.
// ERRORS:   database saves are wrapped in try/catch (DbUpdateException) and the
//           user sees a friendly message instead of a crash.
// =====================================================================

using HealthTrack.Models;                  // Notification, ApplicationUser
using HealthTrack.Repositories.Interfaces; // INotificationRepository
using HealthTrack.Services;                // INotificationService
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult, [HttpPost]
using Microsoft.EntityFrameworkCore;       // DbUpdateException

namespace HealthTrack.Controllers
{
    /// <summary>
    /// Task 8 - Notifications. Every signed-in user (Client, Trainer, Admin) can see,
    /// open, mark as read and delete ONLY their own notifications.
    /// </summary>
    [Authorize]                                   // any signed-in user, any role
    public class NotificationsController : Controller
    {
        private readonly INotificationRepository _notifications;          // notifications table
        private readonly INotificationService _notificationService;       // creates due reminders
        private readonly UserManager<ApplicationUser> _users;             // gets the signed-in user's id
        private readonly ILogger<NotificationsController> _logger;        // writes errors to the log

        // Constructor: the services are injected by ASP.NET Core
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
            var userId = _users.GetUserId(User);          // the signed-in user's id
            if (userId is null) return Challenge();       // not signed in: go to the login page

            // Make sure today's reminders exist before showing the list
            try
            {
                await _notificationService.GenerateRemindersAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not generate reminders in NotificationsController.Index");   // log it; still show the page
            }

            var list = await _notifications.GetForUserAsync(userId);   // newest first
            return View(list.ToList());                                 // show Views/Notifications/Index.cshtml
        }

        // POST: Notifications/Open/5 - marks it as read and opens the related page
        [HttpPost, ValidateAntiForgeryToken]                           // POST only, with a valid anti-forgery token
        public async Task<IActionResult> Open(int id)
        {
            var notification = await GetOwnNotificationAsync(id);      // only the user's own notification
            if (notification is null) return NotFound();               // not found or not theirs: 404

            if (!notification.IsRead)                                  // first time opened
            {
                notification.IsRead = true;                            // mark it as read
                _notifications.Update(notification);
                await SaveSafelyAsync("Open");
            }

            // Only follow links inside HealthTrack (prevents open-redirect attacks)
            if (!string.IsNullOrEmpty(notification.Link) && Url.IsLocalUrl(notification.Link))
            {
                return LocalRedirect(notification.Link);               // go to the related page
            }
            return RedirectToAction(nameof(Index));                    // no link: back to the list
        }

        // POST: Notifications/MarkAsRead/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await GetOwnNotificationAsync(id);      // only the user's own notification
            if (notification is null) return NotFound();               // not found or not theirs: 404

            notification.IsRead = true;                                // mark it as read
            _notifications.Update(notification);
            await SaveSafelyAsync("MarkAsRead");
            return RedirectToAction(nameof(Index));                    // back to the list
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _users.GetUserId(User);                       // the signed-in user's id
            if (userId is null) return Challenge();                    // not signed in

            try
            {
                await _notifications.MarkAllAsReadAsync(userId);       // only this user's notifications
                TempData["Success"] = "All notifications marked as read.";   // green message on the next page
            }
            catch (DbUpdateException ex)                               // the database save failed
            {
                _logger.LogError(ex, "Database save failed in NotificationsController.MarkAllAsRead");
                TempData["Error"] = "Sorry, the notifications could not be updated. Please try again.";
            }
            return RedirectToAction(nameof(Index));                    // back to the list
        }

        // POST: Notifications/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await GetOwnNotificationAsync(id);      // only the user's own notification
            if (notification is null) return NotFound();               // not found or not theirs: 404

            _notifications.Delete(notification);                       // mark it for deletion
            if (await SaveSafelyAsync("Delete"))                       // saved successfully?
            {
                TempData["Success"] = "Notification deleted.";
            }
            return RedirectToAction(nameof(Index));                    // back to the list
        }

        /// <summary>
        /// Loads a notification only if it belongs to the signed-in user.
        /// Returns null otherwise, so users can never read or change other people's notifications.
        /// </summary>
        private async Task<Notification?> GetOwnNotificationAsync(int id)
        {
            var userId = _users.GetUserId(User);                       // the signed-in user's id
            if (userId is null) return null;                           // not signed in

            var notification = await _notifications.GetByIdAsync(id);  // load it by id
            return notification is not null && notification.UserId == userId ? notification : null;   // owner check
        }

        /// <summary>Saves changes; logs the error and shows a friendly message if it fails.</summary>
        private async Task<bool> SaveSafelyAsync(string action)
        {
            try
            {
                await _notifications.SaveAsync();                      // write the changes
                return true;                                           // success
            }
            catch (DbUpdateException ex)                               // the database save failed
            {
                _logger.LogError(ex, "Database save failed in NotificationsController.{Action}", action);
                TempData["Error"] = "Sorry, the notification could not be updated. Please try again.";
                return false;                                          // tell the caller it failed
            }
        }
    }
}
