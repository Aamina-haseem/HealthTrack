// =====================================================================
// FILE:    Repositories/AppointmentRepository.cs
// PURPOSE: Database queries for appointments. Inherits the generic CRUD methods
//          from Repository<Appointment> and adds the lists the appointment pages
//          and dashboards need (by client, by trainer, upcoming, one with details).
// KEY IDEA: Include() loads the related client/trainer in the same SQL query
//           (eager loading), so names can be shown without extra database calls.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // Appointment, AppointmentStatus
using HealthTrack.Repositories.Interfaces; // IAppointmentRepository
using Microsoft.EntityFrameworkCore;       // Include, ToListAsync, FirstOrDefaultAsync

namespace HealthTrack.Repositories
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        // Pass the DbContext to the generic base class
        public AppointmentRepository(ApplicationDbContext context) : base(context) { }

        // A client's appointments, most recent date first
        public async Task<IEnumerable<Appointment>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Include(a => a.Trainer)                     // load the trainer (to show their name)
                .Where(a => a.ClientId == clientId)          // only this client's appointments
                .OrderByDescending(a => a.AppointmentDate)   // latest first
                .ToListAsync();                              // run the query
        }

        // A trainer's appointments, most recent date first
        public async Task<IEnumerable<Appointment>> GetByTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Include(a => a.Client)                      // load the client (to show their name)
                .Where(a => a.TrainerId == trainerId)        // only this trainer's appointments
                .OrderByDescending(a => a.AppointmentDate)   // latest first
                .ToListAsync();                              // run the query
        }

        // Appointments still to come for a user, whether they are the client or the trainer
        public async Task<IEnumerable<Appointment>> GetUpcomingAsync(string userId)
        {
            return await _dbSet
                .Include(a => a.Client)                                       // load the client
                .Include(a => a.Trainer)                                      // load the trainer
                .Where(a => (a.ClientId == userId || a.TrainerId == userId)   // the user is either side
                            && a.AppointmentDate >= DateTime.Now              // not in the past
                            && a.Status != AppointmentStatus.Cancelled)       // not cancelled
                .OrderBy(a => a.AppointmentDate)   // soonest first
                .ToListAsync();                                               // run the query
        }

        // One appointment with both people loaded (details page)
        public async Task<Appointment?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(a => a.Client)                      // load the client
                .Include(a => a.Trainer)                     // load the trainer
                .FirstOrDefaultAsync(a => a.Id == id);       // the matching row, or null
        }
    }
}