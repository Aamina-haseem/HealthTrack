// =====================================================================
// FILE:    Repositories/MealLogRepository.cs
// PURPOSE: Database queries for meal logs. Inherits the generic CRUD methods
//          from Repository<MealLog> and adds: all of a client's meals, and a
//          client's meals between two dates (charts and reports).
// USED BY: LogsController, ProgressController, dashboards.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // MealLog
using HealthTrack.Repositories.Interfaces; // IMealLogRepository
using Microsoft.EntityFrameworkCore;       // ToListAsync

namespace HealthTrack.Repositories
{
    /// <summary>
    /// Data access for meals logged by clients.
    /// </summary>
    public class MealLogRepository : Repository<MealLog>, IMealLogRepository
    {
        // Pass the DbContext to the generic base class
        public MealLogRepository(ApplicationDbContext context) : base(context) { }

        // All of a client's meals, newest first
        public async Task<IEnumerable<MealLog>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Where(m => m.ClientId == clientId)      // only this client's meals
                .OrderByDescending(m => m.Date)          // newest first
                .ToListAsync();                          // run the query
        }

        // A client's meals from one date to another (both included), oldest first
        public async Task<IEnumerable<MealLog>> GetByClientInRangeAsync(string clientId, DateTime from, DateTime to)
        {
            return await _dbSet
                .Where(m => m.ClientId == clientId && m.Date >= from && m.Date <= to)   // this client, inside the range
                                .OrderBy(m => m.Date)                                    // oldest first
                .ToListAsync();                                                          // run the query
        }
    }
}