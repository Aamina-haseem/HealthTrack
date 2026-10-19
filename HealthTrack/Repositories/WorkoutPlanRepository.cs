using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    /// <summary>
    /// Data access for workout plans and their exercises.
    /// </summary>
    public class WorkoutPlanRepository : Repository<WorkoutPlan>, IWorkoutPlanRepository
    {
        public WorkoutPlanRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<WorkoutPlan>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Include(p => p.Trainer)
                .Include(p => p.Items)
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkoutPlan>> GetByTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Include(p => p.Client)
                .Include(p => p.Items)
                .Where(p => p.TrainerId == trainerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<WorkoutPlan?> GetWithItemsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Client)
                .Include(p => p.Trainer)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}