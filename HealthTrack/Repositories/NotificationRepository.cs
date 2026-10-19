using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Notification>> GetForUserAsync(string userId)
        {
            return await _dbSet
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)   // newest first
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unread = await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}