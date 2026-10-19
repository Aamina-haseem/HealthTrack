using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    public class DietPlanRepository : Repository<DietPlan>, IDietPlanRepository
    {
        public DietPlanRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<DietPlan>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Include(p => p.Trainer)
                .Include(p => p.Items)
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<DietPlan>> GetByTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Include(p => p.Client)
                .Include(p => p.Items)
                .Where(p => p.TrainerId == trainerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<DietPlan?> GetWithItemsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Client)
                .Include(p => p.Trainer)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}