// =====================================================================
// FILE:    Repositories/WorkoutLogRepository.cs
// PURPOSE: Database queries for workout logs. Inherits the generic CRUD methods
//          from Repository<WorkoutLog> and adds: all of a client's workouts, and a
//          client's workouts between two dates (weekly charts and reports).
// USED BY: LogsController, ProgressController, dashboards.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // WorkoutLog
using HealthTrack.Repositories.Interfaces; // IWorkoutLogRepository
using Microsoft.EntityFrameworkCore;       // Include, ToListAsync

namespace HealthTrack.Repositories
{
    public class WorkoutLogRepository : Repository<WorkoutLog>, IWorkoutLogRepository
    {
        // Pass the DbContext to the generic base class
        public WorkoutLogRepository(ApplicationDbContext context) : base(context) { }

        // All of a client's workouts, newest first, with the linked plan loaded
        public async Task<IEnumerable<WorkoutLog>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Include(w => w.WorkoutPlan)             // load the plan (to show its title)
                .Where(w => w.ClientId == clientId)      // only this client's workouts
                .OrderByDescending(w => w.Date)          // newest first
                .ToListAsync();                          // run the query
        }

        // A client's workouts from one date to another (both included)
        public async Task<IEnumerable<WorkoutLog>> GetByClientInRangeAsync(string clientId, DateTime from, DateTime to)
        {
            return await _dbSet
                .Where(w => w.ClientId == clientId && w.Date >= from && w.Date <= to)   // this client, inside the range
                .OrderBy(w => w.Date)   // oldest first, so charts read left to right
                .ToListAsync();                                                         // run the query
        }
    }
}