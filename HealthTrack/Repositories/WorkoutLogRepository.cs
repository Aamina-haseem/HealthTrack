using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    public class WorkoutLogRepository : Repository<WorkoutLog>, IWorkoutLogRepository
    {
        public WorkoutLogRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<WorkoutLog>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Include(w => w.WorkoutPlan)
                .Where(w => w.ClientId == clientId)
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkoutLog>> GetByClientInRangeAsync(string clientId, DateTime from, DateTime to)
        {
            return await _dbSet
                .Where(w => w.ClientId == clientId && w.Date >= from && w.Date <= to)
                .OrderBy(w => w.Date)   // oldest first, so charts read left to right
                .ToListAsync();
        }
    }
}