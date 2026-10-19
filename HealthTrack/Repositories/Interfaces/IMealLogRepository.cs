using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for meals logged by clients.
    /// </summary>
    public interface IMealLogRepository : IRepository<MealLog>
    {
        Task<IEnumerable<MealLog>> GetByClientAsync(string clientId);

        Task<IEnumerable<MealLog>> GetByClientInRangeAsync(string clientId, DateTime from, DateTime to);
    }
}