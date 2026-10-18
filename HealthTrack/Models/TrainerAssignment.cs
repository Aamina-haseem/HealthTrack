using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>
    /// Links a Client to the Trainer responsible for them. Created by the Admin.
    /// </summary>
    public class TrainerAssignment
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        [Required]
        public string TrainerId { get; set; } = string.Empty;
        public ApplicationUser? Trainer { get; set; }

        [Display(Name = "Assigned On")]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        // false when the client is moved to another trainer (keeps history)
        public bool IsActive { get; set; } = true;
    }
}