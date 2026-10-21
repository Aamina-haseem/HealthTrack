// =====================================================================
// FILE:    Repositories/NotificationRepository.cs
// PURPOSE: Database queries for notifications. Inherits the generic CRUD methods
//          from Repository<Notification> and adds: a user's notifications, their
//          unread count (the bell badge) and "mark all as read".
// USED BY: NotificationService, NotificationsController, NotificationBell view component.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // Notification
using HealthTrack.Repositories.Interfaces; // INotificationRepository
using Microsoft.EntityFrameworkCore;       // ToListAsync, CountAsync

namespace HealthTrack.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        // Pass the DbContext to the generic base class
        public NotificationRepository(ApplicationDbContext context) : base(context) { }

        // All of a user's notifications
        public async Task<IEnumerable<Notification>> GetForUserAsync(string userId)
        {
            return await _dbSet
                .Where(n => n.UserId == userId)          // only this user's notifications
                .OrderByDescending(n => n.CreatedAt)   // newest first
                .ToListAsync();                          // run the query
        }

        // How many unread notifications the user has
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead);   // SQL COUNT, no rows loaded
        }

        // Marks every unread notification of the user as read
        public async Task MarkAllAsReadAsync(string userId)
        {
            var unread = await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)   // this user's unread notifications
                .ToListAsync();                                // load them

            foreach (var notification in unread)               // for each one...
            {
                notification.IsRead = true;                    // ...mark it as read (EF tracks the change)
            }

            await _context.SaveChangesAsync();                 // save all the changes in one go
        }
    }
}