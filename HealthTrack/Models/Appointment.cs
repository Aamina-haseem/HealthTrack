// =====================================================================
// FILE:    Models/Appointment.cs
// PURPOSE: A consultation that a Client books with a Trainer / health
//          professional. It moves through the statuses Pending -> Approved ->
//          Completed (or Cancelled). The trainer's private notes are encrypted
//          in the database (see ApplicationDbContext).
// USED BY: AppointmentsController, AppointmentRepository, dashboards.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Display]

namespace HealthTrack.Models
{
    /// <summary>
    /// A consultation booked by a Client with a Trainer/health professional.
    /// The Trainer can approve, reschedule or cancel it.
    /// </summary>
    public class Appointment
    {
        public int Id { get; set; }                                  // primary key (EF Core uses "Id" automatically)

        [Required]                                                   // every appointment belongs to a client
        public string ClientId { get; set; } = string.Empty;         // foreign key to the client's user Id
        public ApplicationUser? Client { get; set; }                 // navigation property: the client object

        [Required]                                                   // every appointment is with a trainer
        public string TrainerId { get; set; } = string.Empty;        // foreign key to the trainer's user Id
        public ApplicationUser? Trainer { get; set; }                // navigation property: the trainer object

        [Required, Display(Name = "Date & Time")]                    // required; label "Date & Time"
        public DateTime AppointmentDate { get; set; }                // when the consultation starts

        [Required, StringLength(250)]                                // required, max 250 characters
        public string Reason { get; set; } = string.Empty;           // why the client wants the appointment

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;   // new bookings wait for approval

        [StringLength(500), Display(Name = "Trainer Notes")]         // max 500 characters before encryption
        public string? TrainerNotes { get; set; }                    // private notes, stored encrypted

        public DateTime CreatedAt { get; set; } = DateTime.Now;      // when the booking was made
    }
}