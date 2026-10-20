using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.ViewComponents
{
    /// <summary>
    /// The bell icon in the navbar with the number of unread notifications.
    /// A View Component is a small, reusable piece of UI with its own logic,
    /// so the layout does not need a controller to show the unread count.
    /// </summary>
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationRepository _notifications;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<NotificationBellViewComponent> _logger;

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

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int unreadCount = 0;
            var userId = _users.GetUserId(UserClaimsPrincipal);

            if (userId is not null)
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

            return View(unreadCount);
        }
    }
}
