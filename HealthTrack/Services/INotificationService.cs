using HealthTrack.Models;

namespace HealthTrack.Services
{
    /// <summary>
    /// Creates in-app notifications (Task 8). Controllers use it to tell users about
    /// events (e.g. "appointment approved"), and it generates automatic reminders
    /// for upcoming appointments, planned workouts and meals.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>Saves one notification for a user (shown under the navbar bell).</summary>
        Task SendAsync(string userId, string title, string message, NotificationType type, string? link = null);

        /// <summary>
        /// Creates today's reminders for a user if they do not exist yet:
        /// appointments in the next 24 hours, today's planned workout and today's meals.
        /// Safe to call many times - it never creates duplicates.
        /// </summary>
        Task GenerateRemindersAsync(string userId);
    }
}
