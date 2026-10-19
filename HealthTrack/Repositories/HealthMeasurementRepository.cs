using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    public class HealthMeasurementRepository : Repository<HealthMeasurement>, IHealthMeasurementRepository
    {
        public HealthMeasurementRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<HealthMeasurement>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Where(h => h.ClientId == clientId)
                .OrderBy(h => h.Date)
                .ToListAsync();
        }

        public async Task<HealthMeasurement?> GetLatestForClientAsync(string clientId)
        {
            return await _dbSet
                .Where(h => h.ClientId == clientId)
                .OrderByDescending(h => h.Date)
                .FirstOrDefaultAsync();
        }
    }
}