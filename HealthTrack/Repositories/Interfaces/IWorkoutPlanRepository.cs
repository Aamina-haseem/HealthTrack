// =====================================================================
// FILE:    Repositories/Interfaces/IWorkoutPlanRepository.cs
// PURPOSE: Contract for workout plan data access: the generic CRUD methods plus
//          a client's plans, a trainer's plans and one plan with its exercises.
// IMPLEMENTED BY: Repositories/WorkoutPlanRepository.cs
// =====================================================================

using HealthTrack.Models;   // WorkoutPlan

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for workout plans and their exercises.
    /// </summary>
    public interface IWorkoutPlanRepository : IRepository<WorkoutPlan>   // inherits the generic CRUD methods
    {
        // Plans assigned to a client (Client dashboard)
        Task<IEnumerable<WorkoutPlan>> GetByClientAsync(string clientId);

        // Plans created by a trainer (Trainer dashboard)
        Task<IEnumerable<WorkoutPlan>> GetByTrainerAsync(string trainerId);

        // One plan with all its exercises, client and trainer loaded
        Task<WorkoutPlan?> GetWithItemsAsync(int id);
    }
}