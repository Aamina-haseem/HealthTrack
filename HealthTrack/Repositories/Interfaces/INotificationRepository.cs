// =====================================================================
// FILE:    Repositories/Interfaces/INotificationRepository.cs
// PURPOSE: Contract for notification data access: the generic CRUD methods plus
//          a user's notifications, their unread count (the red bell badge) and
//          "mark all as read".
// IMPLEMENTED BY: Repositories/NotificationRepository.cs
// =====================================================================

using HealthTrack.Models;   // Notification

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for in-app notifications (navbar bell).
    /// </summary>
    public interface INotificationRepository : IRepository<Notification>   // inherits the generic CRUD methods
    {
        Task<IEnumerable<Notification>> GetForUserAsync(string userId);   // a user's notifications, newest first

        Task<int> GetUnreadCountAsync(string userId);                     // number shown on the bell badge

        Task MarkAllAsReadAsync(string userId);                           // sets IsRead = true on all of them
    }
}