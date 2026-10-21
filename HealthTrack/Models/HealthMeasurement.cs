// =====================================================================
// FILE:    Models/HealthMeasurement.cs
// PURPOSE: A health check-in the Client records: weight, height and resting
//          heart rate on a date. BMI is calculated from weight and height
//          whenever it is read, so it is never stored and never out of date.
//          The Notes field is encrypted in the database (see ApplicationDbContext).
// USED BY: LogsController (record/edit), ProgressController (charts), dashboards.
// =====================================================================

using System.ComponentModel.DataAnnotations;                 // [Required], [Range], [Display], [DataType], [StringLength]
using System.ComponentModel.DataAnnotations.Schema;          // [NotMapped]

namespace HealthTrack.Models
{
    /// <summary>
    /// A health check-in recorded by the Client (weight, height, heart rate).
    /// BMI is calculated automatically, not stored.
    /// </summary>
    public class HealthMeasurement
    {
        public int Id { get; set; }                                      // primary key

        [Required]                                                       // every measurement belongs to a client
        public string ClientId { get; set; } = string.Empty;             // foreign key to the client
        public ApplicationUser? Client { get; set; }                     // navigation property

        [DataType(DataType.Date)]                                        // date only, no time
        public DateTime Date { get; set; } = DateTime.Today;             // defaults to today

        [Range(20, 300), Display(Name = "Weight (kg)")]                  // realistic adult range
        public double WeightKg { get; set; }

        [Range(50, 250), Display(Name = "Height (cm)")]                  // realistic range
        public double HeightCm { get; set; }

        [Range(30, 220), Display(Name = "Resting Heart Rate (bpm)")]     // beats per minute
        public int? RestingHeartRate { get; set; }                       // optional

        // Sensitive health notes - will be encrypted before saving (Security task)
        [StringLength(1000)]                                             // max 1000 characters before encryption
        public string? Notes { get; set; }                               // optional

        /// <summary>
        /// BMI = weight(kg) / height(m)². Calculated on the fly, so it is never out of date.
        /// </summary>
        [NotMapped]                                                      // no BMI column in the database
        public double BMI => HeightCm > 0                                // avoid dividing by zero
            ? Math.Round(WeightKg / Math.Pow(HeightCm / 100, 2), 1)      // cm -> m, square it, round to 1 decimal
            : 0;                                                         // no height = no BMI
    }
}