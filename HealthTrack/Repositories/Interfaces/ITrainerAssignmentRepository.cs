using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for client–trainer assignments (used by Admin and dashboards).
    /// </summary>
    public interface ITrainerAssignmentRepository : IRepository<TrainerAssignment>
    {
        // All assignments with client and trainer names loaded (Admin page)
        Task<IEnumerable<TrainerAssignment>> GetAllWithUsersAsync();

        // The client's current trainer, or null if none assigned
        Task<TrainerAssignment?> GetActiveForClientAsync(string clientId);

        // All clients currently assigned to a trainer (Trainer dashboard)
        Task<IEnumerable<ApplicationUser>> GetClientsForTrainerAsync(string trainerId);
    }
}