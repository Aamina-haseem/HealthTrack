// =====================================================================
// FILE:    Repositories/Interfaces/IMealLogRepository.cs
// PURPOSE: Contract for meal log data access: the generic CRUD methods plus a
//          client's meals (all, or only between two dates for charts/reports).
// IMPLEMENTED BY: Repositories/MealLogRepository.cs
// =====================================================================

using HealthTrack.Models;   // MealLog

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for meals logged by clients.
    /// </summary>
    public interface IMealLogRepository : IRepository<MealLog>   // inherits the generic CRUD methods
    {
        Task<IEnumerable<MealLog>> GetByClientAsync(string clientId);   // all of a client's meals, newest first

        // A client's meals between two dates, oldest first (for charts and reports)
        Task<IEnumerable<MealLog>> GetByClientInRangeAsync(string clientId, DateTime from, DateTime to);
    }
}