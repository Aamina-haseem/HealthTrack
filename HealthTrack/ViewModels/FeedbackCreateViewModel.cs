using System.ComponentModel.DataAnnotations;
namespace HealthTrack.ViewModels
{
    public class FeedbackCreateViewModel
    {
        [Required, StringLength(1000)]
        public string Message { get; set; } = string.Empty;
        [Range(1, 5), Display(Name = "Progress Rating (1–5)")]
        public int? Rating { get; set; }
    }
}
