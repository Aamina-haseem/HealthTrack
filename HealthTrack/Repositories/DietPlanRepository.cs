// =====================================================================
// FILE:    Repositories/DietPlanRepository.cs
// PURPOSE: Database queries for diet plans. Inherits the generic CRUD methods
//          from Repository<DietPlan> and adds: a client's plans, a trainer's
//          plans, and one plan with its meals, client and trainer loaded.
// USED BY: DietPlansController, ClientController, dashboards.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // DietPlan
using HealthTrack.Repositories.Interfaces; // IDietPlanRepository
using Microsoft.EntityFrameworkCore;       // Include, ToListAsync, FirstOrDefaultAsync

namespace HealthTrack.Repositories
{
    public class DietPlanRepository : Repository<DietPlan>, IDietPlanRepository
    {
        // Pass the DbContext to the generic base class
        public DietPlanRepository(ApplicationDbContext context) : base(context) { }

        // Plans written for one client, newest start date first
        public async Task<IEnumerable<DietPlan>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Include(p => p.Trainer)                 // load the trainer who wrote it
                .Include(p => p.Items)                   // load the planned meals
                .Where(p => p.ClientId == clientId)      // only this client's plans
                .OrderByDescending(p => p.StartDate)     // newest first
                .ToListAsync();                          // run the query
        }

        // Plans one trainer has written, most recently created first
        public async Task<IEnumerable<DietPlan>> GetByTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Include(p => p.Client)                  // load the client it is for
                .Include(p => p.Items)                   // load the planned meals
                .Where(p => p.TrainerId == trainerId)    // only this trainer's plans
                .OrderByDescending(p => p.CreatedAt)     // newest first
                .ToListAsync();                          // run the query
        }

        // One plan with everything loaded (details page)
        public async Task<DietPlan?> GetWithItemsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Client)                  // load the client
                .Include(p => p.Trainer)                 // load the trainer
                .Include(p => p.Items)                   // load the planned meals
                .FirstOrDefaultAsync(p => p.Id == id);   // the matching plan, or null
        }
    }
}