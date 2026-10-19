using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for in-app notifications (navbar bell).
    /// </summary>
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetForUserAsync(string userId);

        Task<int> GetUnreadCountAsync(string userId);

        Task MarkAllAsReadAsync(string userId);
    }
}