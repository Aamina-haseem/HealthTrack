// =====================================================================
// FILE:    Repositories/HealthMeasurementRepository.cs
// PURPOSE: Database queries for health measurements. Inherits the generic CRUD
//          methods from Repository<HealthMeasurement> and adds: a client's full
//          history in date order (trend charts) and their latest measurement.
// USED BY: ProgressController, ClientController, TrainerController.
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Models;                  // HealthMeasurement
using HealthTrack.Repositories.Interfaces; // IHealthMeasurementRepository
using Microsoft.EntityFrameworkCore;       // ToListAsync, FirstOrDefaultAsync

namespace HealthTrack.Repositories
{
    public class HealthMeasurementRepository : Repository<HealthMeasurement>, IHealthMeasurementRepository
    {
        // Pass the DbContext to the generic base class
        public HealthMeasurementRepository(ApplicationDbContext context) : base(context) { }

        // All of a client's measurements, oldest first (charts read left to right)
        public async Task<IEnumerable<HealthMeasurement>> GetByClientAsync(string clientId)
        {
            return await _dbSet
                .Where(h => h.ClientId == clientId)      // only this client's measurements
                .OrderBy(h => h.Date)                    // oldest first
                .ToListAsync();                          // run the query
        }

        // The client's most recent measurement, or null if they have none
        public async Task<HealthMeasurement?> GetLatestForClientAsync(string clientId)
        {
            return await _dbSet
                .Where(h => h.ClientId == clientId)      // only this client's measurements
                .OrderByDescending(h => h.Date)          // newest first...
                .FirstOrDefaultAsync();                  // ...and take just the first one
        }
    }
}