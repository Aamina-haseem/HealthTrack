using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    public class FeedbackRepository : Repository<Feedback>, IFeedbackRepository
    {
        public FeedbackRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Feedback>> GetForClientAsync(string clientId)
        {
            return await _dbSet
                .Include(f => f.Trainer)
                .Where(f => f.ClientId == clientId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetByTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Include(f => f.Client)
                .Where(f => f.TrainerId == trainerId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
    }
}