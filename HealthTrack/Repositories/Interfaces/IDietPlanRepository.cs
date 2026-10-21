// =====================================================================
// FILE:    Repositories/Interfaces/IDietPlanRepository.cs
// PURPOSE: Contract for diet plan data access: the generic CRUD methods plus
//          queries for a client's plans, a trainer's plans and one plan with its meals.
// IMPLEMENTED BY: Repositories/DietPlanRepository.cs
// =====================================================================

using HealthTrack.Models;   // DietPlan

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for diet plans and their meals.
    /// </summary>
    public interface IDietPlanRepository : IRepository<DietPlan>   // inherits the generic CRUD methods
    {
        Task<IEnumerable<DietPlan>> GetByClientAsync(string clientId);    // plans written for a client

        Task<IEnumerable<DietPlan>> GetByTrainerAsync(string trainerId);  // plans a trainer has written

        // One plan with all its meals, client and trainer loaded
        Task<DietPlan?> GetWithItemsAsync(int id);
    }
}