// =====================================================================
// FILE:    Repositories/Interfaces/ITrainerAssignmentRepository.cs
// PURPOSE: Contract for client <-> trainer assignment data access: the generic
//          CRUD methods plus the queries the Admin page and the trainer pages use
//          (all assignments, a client's current trainer, a trainer's clients).
// IMPLEMENTED BY: Repositories/TrainerAssignmentRepository.cs
// =====================================================================

using HealthTrack.Models;   // TrainerAssignment, ApplicationUser

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for client–trainer assignments (used by Admin and dashboards).
    /// </summary>
    public interface ITrainerAssignmentRepository : IRepository<TrainerAssignment>   // inherits the generic CRUD methods
    {
        // All assignments with client and trainer names loaded (Admin page)
        Task<IEnumerable<TrainerAssignment>> GetAllWithUsersAsync();

        // The client's current trainer, or null if none assigned
        Task<TrainerAssignment?> GetActiveForClientAsync(string clientId);

        // All clients currently assigned to a trainer (Trainer dashboard)
        Task<IEnumerable<ApplicationUser>> GetClientsForTrainerAsync(string trainerId);
    }
}