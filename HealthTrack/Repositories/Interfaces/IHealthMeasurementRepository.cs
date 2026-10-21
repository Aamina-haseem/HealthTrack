// =====================================================================
// FILE:    Repositories/Interfaces/IHealthMeasurementRepository.cs
// PURPOSE: Contract for health measurement data access: the generic CRUD methods
//          plus a client's full history (for trend charts) and their latest entry.
// IMPLEMENTED BY: Repositories/HealthMeasurementRepository.cs
// =====================================================================

using HealthTrack.Models;   // HealthMeasurement

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for client health measurements (weight, height, BMI).
    /// </summary>
    public interface IHealthMeasurementRepository : IRepository<HealthMeasurement>   // inherits the generic CRUD methods
    {
        // All measurements in date order (oldest first) for weight/BMI trend charts
        Task<IEnumerable<HealthMeasurement>> GetByClientAsync(string clientId);

        // The most recent measurement (dashboard "current weight / BMI" cards)
        Task<HealthMeasurement?> GetLatestForClientAsync(string clientId);
    }
}