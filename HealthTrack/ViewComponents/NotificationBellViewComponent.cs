// =====================================================================
// FILE:    ViewComponents/NotificationBellViewComponent.cs
// PURPOSE: The bell icon in the top bar with the number of unread notifications
//          (Task 8). Every page uses it through the layout. Each time it loads it
//          first creates any due reminders, then counts the unread notifications.
// KEY IDEAS: a View Component is a reusable piece of UI with its own logic (like a
//            mini controller); try/catch keeps the page working if this fails.
// VIEW:    Views/Shared/Components/NotificationBell/Default.cshtml
// =====================================================================

using HealthTrack.Models;                  // ApplicationUser
using HealthTrack.Repositories.Interfaces; // INotificationRepository
using HealthTrack.Services;                // INotificationService
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // ViewComponent, IViewComponentResult

namespace HealthTrack.ViewComponents
{
    /// <summary>
    /// The bell icon in the navbar with the number of unread notifications.
    /// A View Component is a small, reusable piece of UI with its own logic,
    /// so the layout does not need a controller to show the unread count.
    /// </summary>
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationRepository _notifications;          // reads the unread count
        private readonly INotificationService _notificationService;       // creates due reminders
        private readonly UserManager<ApplicationUser> _users;             // finds the signed-in user's id
        private readonly ILogger<NotificationBellViewComponent> _logger;  // writes errors to the log

        // Constructor: the services are injected by ASP.NET Core
        public NotificationBellViewComponent(
            INotificationRepository notifications,
            INotificationService notificationService,
            UserManager<ApplicationUser> users,
            ILogger<NotificationBellViewComponent> logger)
        {
            _notifications = notifications;
            _notificationService = notificationService;
            _users = users;
            _logger = logger;
        }

        // Called by the layout with: @await Component.InvokeAsync("NotificationBell")
        public async Task<IViewComponentResult> InvokeAsync()
        {
            int unreadCount = 0;                                      // 0 if the user is not signed in
            var userId = _users.GetUserId(UserClaimsPrincipal);       // the signed-in user's id (or null)

            if (userId is not null)                                   // only for signed-in users
            {
                try
                {
                    // Creates today's reminders if needed (never duplicates), then counts unread ones
                    await _notificationService.GenerateRemindersAsync(userId);
                    unreadCount = await _notifications.GetUnreadCountAsync(userId);
                }
                catch (Exception ex)
                {
                    // The navbar must always load, even if notifications fail
                    _logger.LogError(ex, "Could not load the notification bell");
                }
            }

            return View(unreadCount);                                 // renders Default.cshtml with the number
        }
    }
}
