using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.ViewModels
{
    // ===============================================================
    // Task 4 - Administrator view models
    // ===============================================================

    /// <summary>Numbers and lists shown on the Admin Dashboard.</summary>
    public class AdminDashboardViewModel
    {
        public string Name { get; init; } = string.Empty;

        // User activity
        public int TotalClients { get; init; }
        public int TotalTrainers { get; init; }
        public int ActiveUsers { get; init; }
        public int InactiveUsers { get; init; }
        public int NewUsersLast30Days { get; init; }
        public int UnassignedClients { get; init; }

        // System usage
        public int PendingAppointments { get; init; }
        public int UpcomingAppointments { get; init; }
        public int CompletedAppointments { get; init; }
        public int WorkoutLogsLast7Days { get; init; }
        public int MealLogsLast7Days { get; init; }
        public int MeasurementsLast7Days { get; init; }

        // Chart: number of logs (workouts + meals + measurements) per day for the last 7 days
        public IReadOnlyList<string> ActivityLabels { get; init; } = Array.Empty<string>();
        public IReadOnlyList<int> ActivityCounts { get; init; } = Array.Empty<int>();

        // Trainer assignments overview
        public IReadOnlyList<TrainerWorkloadRow> TrainerWorkload { get; init; } = Array.Empty<TrainerWorkloadRow>();

        public IReadOnlyList<AdminUserRow> RecentUsers { get; init; } = Array.Empty<AdminUserRow>();
    }

    public class TrainerWorkloadRow
    {
        public string TrainerId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Specialization { get; init; }
        public bool IsActive { get; init; }
        public int ClientCount { get; init; }
        public int UpcomingAppointments { get; init; }
    }

    /// <summary>One row in the Users table.</summary>
    public class AdminUserRow
    {
        public string Id { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string? Specialization { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>The Users page with its search and role filter.</summary>
    public class AdminUsersViewModel
    {
        public IReadOnlyList<AdminUserRow> Users { get; init; } = Array.Empty<AdminUserRow>();
        public string? RoleFilter { get; init; }
        public string? Search { get; init; }
        public string CurrentAdminId { get; init; } = string.Empty;
    }

    /// <summary>Form used by the Admin to create a Trainer / health professional account.</summary>
    public class CreateTrainerViewModel
    {
        [Required(ErrorMessage = "Please enter the first name.")]
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the last name.")]
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter an email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a specialization.")]
        [StringLength(50)]
        public string Specialization { get; set; } = string.Empty;

        public string? Gender { get; set; }

        [Required(ErrorMessage = "Please enter a temporary password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Temporary Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>The Trainer Assignments page.</summary>
    public class AssignmentsViewModel
    {
        public IReadOnlyList<TrainerAssignment> ActiveAssignments { get; init; } = Array.Empty<TrainerAssignment>();
        public IReadOnlyList<string> UnassignedClientNames { get; init; } = Array.Empty<string>();
        public int HistoryCount { get; init; }

        [ValidateNever]
        public IEnumerable<SelectListItem> Clients { get; set; } = Enumerable.Empty<SelectListItem>();

        [ValidateNever]
        public IEnumerable<SelectListItem> Trainers { get; set; } = Enumerable.Empty<SelectListItem>();

        public AssignTrainerInput Input { get; set; } = new();
    }

    /// <summary>The small "assign trainer to client" form.</summary>
    public class AssignTrainerInput
    {
        [Required(ErrorMessage = "Please choose a client.")]
        [Display(Name = "Client")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a trainer.")]
        [Display(Name = "Trainer")]
        public string TrainerId { get; set; } = string.Empty;
    }

    /// <summary>Admin Reports: client engagement, popular routines and trainer performance.</summary>
    public class AdminReportViewModel
    {
        public DateTime GeneratedAt { get; init; } = DateTime.Now;
        public IReadOnlyList<ClientEngagementRow> ClientEngagement { get; init; } = Array.Empty<ClientEngagementRow>();
        public IReadOnlyList<PopularActivityRow> PopularActivities { get; init; } = Array.Empty<PopularActivityRow>();
        public IReadOnlyList<PopularActivityRow> PopularWorkoutPlans { get; init; } = Array.Empty<PopularActivityRow>();
        public IReadOnlyList<TrainerPerformanceRow> TrainerPerformance { get; init; } = Array.Empty<TrainerPerformanceRow>();

        // Engagement summary for the doughnut chart
        public int HighEngagement { get; init; }
        public int MediumEngagement { get; init; }
        public int LowEngagement { get; init; }
        public int InactiveClients { get; init; }
    }

    public class ClientEngagementRow
    {
        public string Name { get; init; } = string.Empty;
        public string TrainerName { get; init; } = "Not assigned";
        public int WorkoutsLast30Days { get; init; }
        public int MealsLast30Days { get; init; }
        public int MeasurementsLast30Days { get; init; }
        public int CaloriesBurnedLast30Days { get; init; }
        public DateTime? LastActivity { get; init; }
        public string EngagementLevel { get; init; } = "Inactive";
    }

    public class TrainerPerformanceRow
    {
        public string Name { get; init; } = string.Empty;
        public string? Specialization { get; init; }
        public int Clients { get; init; }
        public int WorkoutPlans { get; init; }
        public int DietPlans { get; init; }
        public int AppointmentsCompleted { get; init; }
        public int FeedbackGiven { get; init; }
        public double? AverageRating { get; init; }
    }
}
