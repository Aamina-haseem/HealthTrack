// =====================================================================
// FILE:    Models/TrainerAssignment.cs
// PURPOSE: Links a Client to the Trainer responsible for them. Only the Admin
//          creates these. A trainer can only see and manage clients that have an
//          ACTIVE assignment to them, which is how the app keeps client data private.
// USED BY: AdminController (Assignments), TrainerController, plan controllers,
//          TrainerAssignmentRepository.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [Display]

namespace HealthTrack.Models
{
    /// <summary>
    /// Links a Client to the Trainer responsible for them. Created by the Admin.
    /// </summary>
    public class TrainerAssignment
    {
        public int Id { get; set; }                                      // primary key

        [Required]                                                       // the client being assigned
        public string ClientId { get; set; } = string.Empty;             // foreign key to the client
        public ApplicationUser? Client { get; set; }                     // navigation property

        [Required]                                                       // the trainer responsible
        public string TrainerId { get; set; } = string.Empty;            // foreign key to the trainer
        public ApplicationUser? Trainer { get; set; }                    // navigation property

        [Display(Name = "Assigned On")]                                  // label on admin pages
        public DateTime AssignedDate { get; set; } = DateTime.Now;       // when the link was made

        // false when the client is moved to another trainer (keeps history)
        public bool IsActive { get; set; } = true;
    }
}