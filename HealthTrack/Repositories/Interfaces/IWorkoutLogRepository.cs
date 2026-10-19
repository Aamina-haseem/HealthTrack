using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for workouts logged by clients.
    /// </summary>
    public interface IWorkoutLogRepository : IRepository<WorkoutLog>
    {
        // All workouts logged by a client, newest first
        Task<IEnumerable<WorkoutLog>> GetByClientAsync(string clientId);

        // Workouts within a date range (used for weekly charts and reports)
        Task<IEnumerable<WorkoutLog>> GetByClientInRangeAsync(string clientId, DateTime from, DateTime to);
    }
}