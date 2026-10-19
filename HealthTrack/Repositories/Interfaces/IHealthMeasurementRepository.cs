using HealthTrack.Models;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Data access for client health measurements (weight, height, BMI).
    /// </summary>
    public interface IHealthMeasurementRepository : IRepository<HealthMeasurement>
    {
        // All measurements in date order (oldest first) for weight/BMI trend charts
        Task<IEnumerable<HealthMeasurement>> GetByClientAsync(string clientId);

        // The most recent measurement (dashboard "current weight / BMI" cards)
        Task<HealthMeasurement?> GetLatestForClientAsync(string clientId);
    }
}