using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    public class TrainerAssignmentRepository : Repository<TrainerAssignment>, ITrainerAssignmentRepository
    {
        public TrainerAssignmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<TrainerAssignment>> GetAllWithUsersAsync()
        {
            // Include() loads the related users in the same query (avoids extra DB calls)
            return await _dbSet
                .Include(a => a.Client)
                .Include(a => a.Trainer)
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync();
        }

        public async Task<TrainerAssignment?> GetActiveForClientAsync(string clientId)
        {
            return await _dbSet
                .Include(a => a.Trainer)
                .FirstOrDefaultAsync(a => a.ClientId == clientId && a.IsActive);
        }

        public async Task<IEnumerable<ApplicationUser>> GetClientsForTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Where(a => a.TrainerId == trainerId && a.IsActive)
                .Select(a => a.Client!)
                .ToListAsync();
        }
    }
}