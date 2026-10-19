using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for trainer feedback on client progress.
    /// </summary>
    public interface IFeedbackRepository : IRepository<Feedback>
    {
        // Feedback a client has received (Client dashboard)
        Task<IEnumerable<Feedback>> GetForClientAsync(string clientId);

        // Feedback a trainer has written (Trainer dashboard)
        Task<IEnumerable<Feedback>> GetByTrainerAsync(string trainerId);
    }
}