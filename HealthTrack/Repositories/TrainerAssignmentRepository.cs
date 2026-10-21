// =====================================================================
// FILE:    Repositories/TrainerAssignmentRepository.cs
// PURPOSE: Database queries for client <-> trainer assignments. Inherits the
//          generic CRUD methods from Repository<TrainerAssignment> and adds:
//          all assignments with names (Admin page), a client's current trainer,
//          and the clients currently assigned to a trainer.
// KEY IDEA: only ACTIVE assignments count, which is how a trainer is limited to
//           their own clients everywhere in the app.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // TrainerAssignment, ApplicationUser
using HealthTrack.Repositories.Interfaces; // ITrainerAssignmentRepository
using Microsoft.EntityFrameworkCore;       // Include, ToListAsync, FirstOrDefaultAsync

namespace HealthTrack.Repositories
{
    public class TrainerAssignmentRepository : Repository<TrainerAssignment>, ITrainerAssignmentRepository
    {
        // Pass the DbContext to the generic base class
        public TrainerAssignmentRepository(ApplicationDbContext context) : base(context) { }

        // Every assignment (active and old) with both names, newest first
        public async Task<IEnumerable<TrainerAssignment>> GetAllWithUsersAsync()
        {
            // Include() loads the related users in the same query (avoids extra DB calls)
            return await _dbSet
                .Include(a => a.Client)                      // load the client
                .Include(a => a.Trainer)                     // load the trainer
                .OrderByDescending(a => a.AssignedDate)      // newest first
                .ToListAsync();                              // run the query
        }

        // The client's current (active) assignment, or null if they have no trainer
        public async Task<TrainerAssignment?> GetActiveForClientAsync(string clientId)
        {
            return await _dbSet
                .Include(a => a.Trainer)                                             // load the trainer
                .FirstOrDefaultAsync(a => a.ClientId == clientId && a.IsActive);     // the active one only
        }

        // The users (clients) currently assigned to a trainer
        public async Task<IEnumerable<ApplicationUser>> GetClientsForTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Where(a => a.TrainerId == trainerId && a.IsActive)   // this trainer's active assignments
                .Select(a => a.Client!)                               // keep only the client user
                .ToListAsync();                                       // run the query
        }
    }
}