// =====================================================================
// FILE:    Repositories/WorkoutPlanRepository.cs
// PURPOSE: Database queries for workout plans. Inherits the generic CRUD methods
//          from Repository<WorkoutPlan> and adds: a client's plans, a trainer's
//          plans, and one plan with its exercises, client and trainer loaded.
// USED BY: WorkoutPlansController, ClientController, dashboards.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // WorkoutPlan
using HealthTrack.Repositories.Interfaces; // IWorkoutPlanRepository
using Microsoft.EntityFrameworkCore;       // Include, ToListAsync, FirstOrDefaultAsync

namespace HealthTrack.Repositories
{
    /// <summary>
    /// Data access for workout plans and their exercises.
    /// </summary>
    public class WorkoutPlanRepository : Repository<WorkoutPlan>, IWorkoutPlanRepository
    {
        // Pass the DbContext to the generic base class
        public WorkoutPlanRepository(ApplicationDbContext context) : base(context) { }

        // Plans written for one client, newest start date first
        public async Task<IEnumerable<WorkoutPlan>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Include(p => p.Trainer)                 // load the trainer who wrote it
                .Include(p => p.Items)                   // load the exercises
                .Where(p => p.ClientId == clientId)      // only this client's plans
                .OrderByDescending(p => p.StartDate)     // newest first
                .ToListAsync();                          // run the query
        }

        // Plans one trainer has written, most recently created first
        public async Task<IEnumerable<WorkoutPlan>> GetByTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Include(p => p.Client)                  // load the client it is for
                .Include(p => p.Items)                   // load the exercises
                .Where(p => p.TrainerId == trainerId)    // only this trainer's plans
                .OrderByDescending(p => p.CreatedAt)     // newest first
                .ToListAsync();                          // run the query
        }

        // One plan with everything loaded (details page)
        public async Task<WorkoutPlan?> GetWithItemsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Client)                  // load the client
                .Include(p => p.Trainer)                 // load the trainer
                .Include(p => p.Items)                   // load the exercises
                .FirstOrDefaultAsync(p => p.Id == id);   // the matching plan, or null
        }
    }
}