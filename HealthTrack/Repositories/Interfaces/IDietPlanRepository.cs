using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for diet plans and their meals.
    /// </summary>
    public interface IDietPlanRepository : IRepository<DietPlan>
    {
        Task<IEnumerable<DietPlan>> GetByClientAsync(string clientId);

        Task<IEnumerable<DietPlan>> GetByTrainerAsync(string trainerId);

        // One plan with all its meals, client and trainer loaded
        Task<DietPlan?> GetWithItemsAsync(int id);
    }
}