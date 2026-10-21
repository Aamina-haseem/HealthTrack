// =====================================================================
// FILE:    Models/Notification.cs
// PURPOSE: An in-app alert for one user, shown under the bell in the top bar
//          (e.g. "Appointment approved", "New feedback"). It can link to the
//          page it is about and is marked as read when opened.
// USED BY: NotificationService (creates them), NotificationsController,
//          the NotificationBell view component.
// =====================================================================

using System.ComponentModel.DataAnnotations;   // [Required], [StringLength]

namespace HealthTrack.Models
{
    /// <summary>
    /// An in-app reminder or alert shown in the navbar bell (e.g. upcoming appointment).
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }                                    // primary key

        // Who receives the notification
        [Required]                                                     // every notification has a receiver
        public string UserId { get; set; } = string.Empty;             // foreign key to the user
        public ApplicationUser? User { get; set; }                     // navigation property

        [Required, StringLength(100)]                                  // required, max 100 characters
        public string Title { get; set; } = string.Empty;              // short heading, e.g. "Appointment approved"

        [Required, StringLength(500)]                                  // required, max 500 characters
        public string Message { get; set; } = string.Empty;            // the full text

        public NotificationType Type { get; set; }                     // Workout, Meal, Appointment, Feedback or System

        public bool IsRead { get; set; } = false;                      // unread until the user opens it

        public DateTime CreatedAt { get; set; } = DateTime.Now;        // when it was created

        // Optional page to open when clicked, e.g. "/Appointments/Details/5"
        [StringLength(200)]                                            // max 200 characters
        public string? Link { get; set; }
    }
}