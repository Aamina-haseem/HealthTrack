// =====================================================================
// FILE:    Repositories/Interfaces/IFeedbackRepository.cs
// PURPOSE: Contract for trainer feedback data access: the generic CRUD methods
//          plus the feedback a client received and the feedback a trainer wrote.
// IMPLEMENTED BY: Repositories/FeedbackRepository.cs
// =====================================================================

using HealthTrack.Models;   // Feedback

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for trainer feedback on client progress.
    /// </summary>
    public interface IFeedbackRepository : IRepository<Feedback>   // inherits the generic CRUD methods
    {
        // Feedback a client has received (Client dashboard)
        Task<IEnumerable<Feedback>> GetForClientAsync(string clientId);

        // Feedback a trainer has written (Trainer dashboard)
        Task<IEnumerable<Feedback>> GetByTrainerAsync(string trainerId);
    }
}