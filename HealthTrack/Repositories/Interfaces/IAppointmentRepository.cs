using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for appointments between clients and trainers.
    /// </summary>
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<IEnumerable<Appointment>> GetByClientAsync(string clientId);

        Task<IEnumerable<Appointment>> GetByTrainerAsync(string trainerId);

        // Future, non-cancelled appointments for a client OR trainer (dashboards, reminders)
        Task<IEnumerable<Appointment>> GetUpcomingAsync(string userId);

        // One appointment with client and trainer details loaded
        Task<Appointment?> GetWithDetailsAsync(int id);
    }
}