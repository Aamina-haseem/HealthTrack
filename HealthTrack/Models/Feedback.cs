using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// Feedback written by a Trainer about a Client's progress.
    /// </summary>
    public class Feedback
    {
        public int Id { get; set; }

        [Required]
        public string TrainerId { get; set; } = string.Empty;
        public ApplicationUser? Trainer { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        [Required, StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Range(1, 5), Display(Name = "Progress Rating (1-5)")]
        public int? Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}