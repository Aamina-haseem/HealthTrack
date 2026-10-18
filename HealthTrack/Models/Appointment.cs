using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// A consultation booked by a Client with a Trainer/health professional.
    /// The Trainer can approve, reschedule or cancel it.
    /// </summary>
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        [Required]
        public string TrainerId { get; set; } = string.Empty;
        public ApplicationUser? Trainer { get; set; }

        [Required, Display(Name = "Date & Time")]
        public DateTime AppointmentDate { get; set; }

        [Required, StringLength(250)]
        public string Reason { get; set; } = string.Empty;

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [StringLength(500), Display(Name = "Trainer Notes")]
        public string? TrainerNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}