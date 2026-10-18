using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// An in-app reminder or alert shown in the navbar bell (e.g. upcoming appointment).
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }

        // Who receives the notification
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Optional page to open when clicked, e.g. "/Appointments/Details/5"
        [StringLength(200)]
        public string? Link { get; set; }
    }
}