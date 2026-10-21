// =====================================================================
// FILE:    Models/Feedback.cs
// PURPOSE: A message a Trainer writes about one of their Client's progress,
//          with an optional 1-5 progress rating. The client sees it on their
//          dashboard and gets a notification.
// USED BY: TrainerController.GiveFeedback, ClientController (dashboard).
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Range], [Display]

namespace HealthTrack.Models
{
    /// <summary>
    /// Feedback written by a Trainer about a Client's progress.
    /// </summary>
    public class Feedback
    {
        public int Id { get; set; }                                     // primary key

        [Required]                                                      // who wrote it
        public string TrainerId { get; set; } = string.Empty;           // foreign key to the trainer
        public ApplicationUser? Trainer { get; set; }                   // navigation property

        [Required]                                                      // who it is about
        public string ClientId { get; set; } = string.Empty;            // foreign key to the client
        public ApplicationUser? Client { get; set; }                    // navigation property

        [Required, StringLength(1000)]                                  // required, max 1000 characters
        public string Message { get; set; } = string.Empty;             // the feedback text

        [Range(1, 5), Display(Name = "Progress Rating (1-5)")]          // 1 to 5 when given
        public int? Rating { get; set; }                                // optional (nullable)

        public DateTime CreatedAt { get; set; } = DateTime.Now;         // when it was written
    }
}