// =====================================================================
// FILE:    ViewModels/AdminViewModels.cs
// PURPOSE: All the view models for the Administrator pages (Task 4): the Admin
//          dashboard numbers and charts, the Users page, the "Add Trainer" form,
//          the Trainer Assignments page and the Admin Reports page.
// KEY IDEAS: a view model carries exactly the data one page needs (not the whole
//            database entity); "init" properties are set once by the controller;
//            data annotations validate the forms on the server.
// USED BY: AdminController and the Views/Admin pages.
// =====================================================================

using System.ComponentModel.DataAnnotations;              // [Required], [StringLength], [Compare], ...
using HealthTrack.Models;                                 // TrainerAssignment
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;   // [ValidateNever]
using Microsoft.AspNetCore.Mvc.Rendering;                 // SelectListItem (drop-down options)

namespace HealthTrack.ViewModels
{
    // ===============================================================
    // Task 4 - Administrator view models
    // ===============================================================

    /// <summary>Numbers and lists shown on the Admin Dashboard.</summary>
    public class AdminDashboardViewModel
    {
        public string Name { get; init; } = string.Empty;          // the admin's name for "Welcome back, ..."

        // User activity
        public int TotalClients { get; init; }                     // number of client accounts
        public int TotalTrainers { get; init; }                    // number of trainer accounts
        public int ActiveUsers { get; init; }                      // accounts with IsActive = true
        public int InactiveUsers { get; init; }                    // accounts the admin deactivated
        public int NewUsersLast30Days { get; init; }               // accounts created in the last 30 days
        public int UnassignedClients { get; init; }                // active clients with no trainer yet

        // System usage
        public int PendingAppointments { get; init; }              // waiting for a trainer's approval
        public int UpcomingAppointments { get; init; }             // approved and still to come
        public int CompletedAppointments { get; init; }            // already finished
        public int WorkoutLogsLast7Days { get; init; }             // workouts logged this week
        public int MealLogsLast7Days { get; init; }                // meals logged this week
        public int MeasurementsLast7Days { get; init; }            // measurements recorded this week

        // Chart: number of logs (workouts + meals + measurements) per day for the last 7 days
        public IReadOnlyList<string> ActivityLabels { get; init; } = Array.Empty<string>();   // x axis: day names
        public IReadOnlyList<int> ActivityCounts { get; init; } = Array.Empty<int>();         // y axis: logs per day

        // Trainer assignments overview
        public IReadOnlyList<TrainerWorkloadRow> TrainerWorkload { get; init; } = Array.Empty<TrainerWorkloadRow>();

        public IReadOnlyList<AdminUserRow> RecentUsers { get; init; } = Array.Empty<AdminUserRow>();   // newest accounts table
    }

    // One trainer's workload (used for the "Clients per Trainer" chart and table)
    public class TrainerWorkloadRow
    {
        public string TrainerId { get; init; } = string.Empty;     // the trainer's user Id
        public string Name { get; init; } = string.Empty;          // full name
        public string? Specialization { get; init; }               // e.g. "Nutritionist"
        public bool IsActive { get; init; }                        // whether the account is active
        public int ClientCount { get; init; }                      // active clients assigned to them
        public int UpcomingAppointments { get; init; }             // their approved future appointments
    }

    /// <summary>One row in the Users table.</summary>
    public class AdminUserRow
    {
        public string Id { get; init; } = string.Empty;            // user Id (used by the activate/deactivate button)
        public string FullName { get; init; } = string.Empty;      // first + last name
        public string Email { get; init; } = string.Empty;         // login email
        public string Role { get; init; } = string.Empty;          // Client, Trainer or Admin
        public string? Specialization { get; init; }               // trainers only
        public bool IsActive { get; init; }                        // active or deactivated
        public DateTime CreatedAt { get; init; }                   // when the account was created
    }

    /// <summary>The Users page with its search and role filter.</summary>
    public class AdminUsersViewModel
    {
        public IReadOnlyList<AdminUserRow> Users { get; init; } = Array.Empty<AdminUserRow>();   // the rows to show
        public string? RoleFilter { get; init; }                   // the selected role tab (null = all)
        public string? Search { get; init; }                       // the text typed in the search box
        public string CurrentAdminId { get; init; } = string.Empty; // the signed-in admin (cannot deactivate themselves)
    }

    /// <summary>Form used by the Admin to create a Trainer / health professional account.</summary>
    public class CreateTrainerViewModel
    {
        [Required(ErrorMessage = "Please enter the first name.")]                                              // cannot be empty
        [StringLength(50)]                                                                                     // max 50 characters
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters.")]         // letters, spaces, ' and - only
        [Display(Name = "First Name")]                                                                         // form label
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the last name.")]                                               // cannot be empty
        [StringLength(50)]                                                                                     // max 50 characters
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters.")]          // letters, spaces, ' and - only
        [Display(Name = "Last Name")]                                                                          // form label
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter an email address.")]                                            // cannot be empty
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]                                   // must look like an email
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a specialization.")]                                           // must pick one
        [StringLength(50)]                                                                                     // max 50 characters
        public string Specialization { get; set; } = string.Empty;

        public string? Gender { get; set; }                                                                    // optional

        [Required(ErrorMessage = "Please enter a temporary password.")]                                        // cannot be empty
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")] // 8 to 100 characters
        [DataType(DataType.Password)]                                                                          // shown as a password box
        [Display(Name = "Temporary Password")]                                                                 // form label
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]                                                                          // shown as a password box
        [Display(Name = "Confirm Password")]                                                                   // form label
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]     // must equal Password
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>The Trainer Assignments page.</summary>
    public class AssignmentsViewModel
    {
        public IReadOnlyList<TrainerAssignment> ActiveAssignments { get; init; } = Array.Empty<TrainerAssignment>();   // current links
        public IReadOnlyList<string> UnassignedClientNames { get; init; } = Array.Empty<string>();                      // clients with no trainer
        public int HistoryCount { get; init; }                     // how many old (inactive) assignments exist

        // Drop-down options; filled by the controller, never posted back, so not validated
        [ValidateNever]
        public IEnumerable<SelectListItem> Clients { get; set; } = Enumerable.Empty<SelectListItem>();

        [ValidateNever]
        public IEnumerable<SelectListItem> Trainers { get; set; } = Enumerable.Empty<SelectListItem>();

        public AssignTrainerInput Input { get; set; } = new();     // the small form on the page
    }

    /// <summary>The small "assign trainer to client" form.</summary>
    public class AssignTrainerInput
    {
        [Required(ErrorMessage = "Please choose a client.")]       // must pick a client
        [Display(Name = "Client")]                                 // form label
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a trainer.")]      // must pick a trainer
        [Display(Name = "Trainer")]                                // form label
        public string TrainerId { get; set; } = string.Empty;
    }

    /// <summary>Admin Reports: client engagement, popular routines and trainer performance.</summary>
    public class AdminReportViewModel
    {
        public DateTime GeneratedAt { get; init; } = DateTime.Now;                                                          // shown as "Generated on ..."
        public IReadOnlyList<ClientEngagementRow> ClientEngagement { get; init; } = Array.Empty<ClientEngagementRow>();     // one row per client
        public IReadOnlyList<PopularActivityRow> PopularActivities { get; init; } = Array.Empty<PopularActivityRow>();      // most logged activities
        public IReadOnlyList<PopularActivityRow> PopularWorkoutPlans { get; init; } = Array.Empty<PopularActivityRow>();    // most used plans
        public IReadOnlyList<TrainerPerformanceRow> TrainerPerformance { get; init; } = Array.Empty<TrainerPerformanceRow>(); // one row per trainer

        // Engagement summary for the doughnut chart
        public int HighEngagement { get; init; }                   // clients in the "High" group
        public int MediumEngagement { get; init; }                 // clients in the "Medium" group
        public int LowEngagement { get; init; }                    // clients in the "Low" group
        public int InactiveClients { get; init; }                  // clients with no activity in 30 days
    }

    // One client's activity over the last 30 days (Admin Reports table)
    public class ClientEngagementRow
    {
        public string Name { get; init; } = string.Empty;           // client name
        public string TrainerName { get; init; } = "Not assigned";  // their trainer, or "Not assigned"
        public int WorkoutsLast30Days { get; init; }                // workouts logged
        public int MealsLast30Days { get; init; }                   // meals logged
        public int MeasurementsLast30Days { get; init; }            // measurements recorded
        public int CaloriesBurnedLast30Days { get; init; }          // total calories burned
        public DateTime? LastActivity { get; init; }                // most recent log of any kind
        public string EngagementLevel { get; init; } = "Inactive";  // High, Medium, Low or Inactive
    }

    // One trainer's results (Admin Reports table)
    public class TrainerPerformanceRow
    {
        public string Name { get; init; } = string.Empty;           // trainer name
        public string? Specialization { get; init; }                // e.g. "Fitness Trainer"
        public int Clients { get; init; }                           // active clients
        public int WorkoutPlans { get; init; }                      // workout plans written
        public int DietPlans { get; init; }                         // diet plans written
        public int AppointmentsCompleted { get; init; }             // appointments finished
        public int FeedbackGiven { get; init; }                     // feedback messages written
        public double? AverageRating { get; init; }                 // average progress rating given (null if none)
    }
}
