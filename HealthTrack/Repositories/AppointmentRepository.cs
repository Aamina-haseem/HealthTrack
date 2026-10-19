using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Repositories
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Appointment>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Include(a => a.Trainer)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetByTrainerAsync(string trainerId)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.TrainerId == trainerId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetUpcomingAsync(string userId)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Include(a => a.Trainer)
                .Where(a => (a.ClientId == userId || a.TrainerId == userId)
                            && a.AppointmentDate >= DateTime.Now
                            && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDate)   // soonest first
                .ToListAsync();
        }

        public async Task<Appointment?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Include(a => a.Trainer)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
    }
}