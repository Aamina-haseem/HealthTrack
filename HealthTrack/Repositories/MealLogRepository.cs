using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    /// <summary>
    /// Data access for meals logged by clients.
    /// </summary>
    public class MealLogRepository : Repository<MealLog>, IMealLogRepository
    {
        public MealLogRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MealLog>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<MealLog>> GetByClientInRangeAsync(string clientId, DateTime from, DateTime to)
        {
            return await _dbSet
                .Where(m => m.ClientId == clientId && m.Date >= from && m.Date <= to)
                                .OrderBy(m => m.Date)
                .ToListAsync();
        }
    }
}