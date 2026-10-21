// =====================================================================
// FILE:    ViewModels/FeedbackCreateViewModel.cs
// PURPOSE: The form fields a trainer fills in to give feedback to a client: the
//          message and an optional 1-5 progress rating. The trainer and client
//          ids come from the signed-in user and the URL, and are checked on the server.
// USED BY: TrainerController.GiveFeedback and Views/Trainer/GiveFeedback.cshtml (Task 3).
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength], [Range], [Display]
namespace HealthTrack.ViewModels
{
    public class FeedbackCreateViewModel
    {
        [Required, StringLength(1000)]                               // required, max 1000 characters
        public string Message { get; set; } = string.Empty;
        [Range(1, 5), Display(Name = "Progress Rating (1–5)")]       // 1 to 5 when given
        public int? Rating { get; set; }                             // optional
    }
}
