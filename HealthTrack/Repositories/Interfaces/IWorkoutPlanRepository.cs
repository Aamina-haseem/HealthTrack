using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for workout plans and their exercises.
    /// </summary>
    public interface IWorkoutPlanRepository : IRepository<WorkoutPlan>
    {
        // Plans assigned to a client (Client dashboard)
        Task<IEnumerable<WorkoutPlan>> GetByClientAsync(string clientId);

        // Plans created by a trainer (Trainer dashboard)
        Task<IEnumerable<WorkoutPlan>> GetByTrainerAsync(string trainerId);

        // One plan with all its exercises, client and trainer loaded
        Task<WorkoutPlan?> GetWithItemsAsync(int id);
    }
}