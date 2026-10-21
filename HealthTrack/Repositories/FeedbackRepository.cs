// =====================================================================
// FILE:    Repositories/FeedbackRepository.cs
// PURPOSE: Database queries for trainer feedback. Inherits the generic CRUD
//          methods from Repository<Feedback> and adds: feedback a client received
//          and feedback a trainer wrote, newest first.
// USED BY: ClientController (dashboard), TrainerController.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // Feedback
using HealthTrack.Repositories.Interfaces; // IFeedbackRepository
using Microsoft.EntityFrameworkCore;       // Include, ToListAsync

namespace HealthTrack.Repositories
{
    public class FeedbackRepository : Repository<Feedback>, IFeedbackRepository
    {
        // Pass the DbContext to the generic base class
        public FeedbackRepository(ApplicationDbContext context) : base(context) { }

        // Feedback a client has received
        public async Task<IEnumerable<Feedback>> GetForClientAsync(string clientId)
        {
            return await _dbSet
                .Include(f => f.Trainer)                 // load the trainer who wrote it
                .Where(f => f.ClientId == clientId)      // only feedback about this client
                .OrderByDescending(f => f.CreatedAt)     // newest first
                .ToListAsync();                          // run the query
        }

        // Feedback a trainer has written
        public async Task<IEnumerable<Feedback>> GetByTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Include(f => f.Client)                  // load the client it is about
                .Where(f => f.TrainerId == trainerId)    // only this trainer's feedback
                .OrderByDescending(f => f.CreatedAt)     // newest first
                .ToListAsync();                          // run the query
        }
    }
}