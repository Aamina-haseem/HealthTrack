// =====================================================================
// FILE:    Repositories/Interfaces/IWorkoutLogRepository.cs
// PURPOSE: Contract for workout log data access: the generic CRUD methods plus a
//          client's workouts (all, or only between two dates for charts/reports).
// IMPLEMENTED BY: Repositories/WorkoutLogRepository.cs
// =====================================================================

using HealthTrack.Models;   // WorkoutLog

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for workouts logged by clients.
    /// </summary>
    public interface IWorkoutLogRepository : IRepository<WorkoutLog>   // inherits the generic CRUD methods
    {
        // All workouts logged by a client, newest first
        Task<IEnumerable<WorkoutLog>> GetByClientAsync(string clientId);

        // Workouts within a date range (used for weekly charts and reports)
        Task<IEnumerable<WorkoutLog>> GetByClientInRangeAsync(string clientId, DateTime from, DateTime to);
    }
}