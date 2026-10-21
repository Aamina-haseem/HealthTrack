// =====================================================================
// FILE:    Repositories/Interfaces/IAppointmentRepository.cs
// PURPOSE: Contract for appointment data access. It has all the generic CRUD
//          methods (it extends IRepository<Appointment>) plus the extra queries
//          the appointment pages and dashboards need.
// IMPLEMENTED BY: Repositories/AppointmentRepository.cs
// =====================================================================

using HealthTrack.Models;   // Appointment

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for appointments between clients and trainers.
    /// </summary>
    public interface IAppointmentRepository : IRepository<Appointment>   // inherits GetAll, GetById, Add, ...
    {
        Task<IEnumerable<Appointment>> GetByClientAsync(string clientId);    // a client's appointments

        Task<IEnumerable<Appointment>> GetByTrainerAsync(string trainerId);  // a trainer's appointments

        // Future, non-cancelled appointments for a client OR trainer (dashboards, reminders)
        Task<IEnumerable<Appointment>> GetUpcomingAsync(string userId);

        // One appointment with client and trainer details loaded
        Task<Appointment?> GetWithDetailsAsync(int id);
    }
}