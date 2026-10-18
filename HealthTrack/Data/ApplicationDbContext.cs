using HealthTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Data
{
    /// <summary>
    /// The EF Core database context. Inherits the Identity tables
    /// (users, roles, logins) and holds all HealthTrack tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Each DbSet becomes a table in SQL Server
        public DbSet<TrainerAssignment> TrainerAssignments { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutPlanItem> WorkoutPlanItems { get; set; }
        public DbSet<DietPlan> DietPlans { get; set; }
        public DbSet<DietPlanItem> DietPlanItems { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<MealLog> MealLogs { get; set; }
        public DbSet<HealthMeasurement> HealthMeasurements { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Must be called first so Identity can configure its own tables
            base.OnModelCreating(builder);

            // Many tables link to ApplicationUser twice (ClientId and TrainerId).
            // SQL Server does not allow multiple cascade-delete paths, so for our
            // HealthTrack tables we use Restrict: a user with health data cannot be
            // hard-deleted; the Admin deactivates them instead (IsActive = false),
            // which also protects the client's health records.
            var userForeignKeys = builder.Model.GetEntityTypes()
                .Where(e => e.ClrType.Namespace == "HealthTrack.Models")
                .SelectMany(e => e.GetForeignKeys())
                .Where(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser));

            foreach (var foreignKey in userForeignKeys)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}