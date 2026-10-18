using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>
    /// A health check-in recorded by the Client (weight, height, heart rate).
    /// BMI is calculated automatically, not stored.
    /// </summary>
    public class HealthMeasurement
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Range(20, 300), Display(Name = "Weight (kg)")]
        public double WeightKg { get; set; }

        [Range(50, 250), Display(Name = "Height (cm)")]
        public double HeightCm { get; set; }

        [Range(30, 220), Display(Name = "Resting Heart Rate (bpm)")]
        public int? RestingHeartRate { get; set; }

        // Sensitive health notes - will be encrypted before saving (Security task)
        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// BMI = weight(kg) / height(m)². Calculated on the fly, so it is never out of date.
        /// </summary>
        [NotMapped]
        public double BMI => HeightCm > 0
            ? Math.Round(WeightKg / Math.Pow(HeightCm / 100, 2), 1)
            : 0;
    }
}