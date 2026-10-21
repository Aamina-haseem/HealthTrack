// =====================================================================
// FILE:    Data/ApplicationDbContext.cs
// PURPOSE: The Entity Framework Core "database context": the bridge between the
//          C# model classes and the SQL Server database. Each DbSet below is one
//          table. It also inherits all the Identity tables (users, roles, logins).
//          OnModelCreating adds two rules:
//            1. user foreign keys use Restrict (no cascade delete of health data)
//            2. sensitive text columns are encrypted automatically (value converter)
// KEY IDEAS: ORM (EF Core), code-first migrations, IdentityDbContext,
//            encryption at rest with the Data Protection API.
// =====================================================================

using HealthTrack.Models;                                   // the model classes (tables)
using Microsoft.AspNetCore.DataProtection;                  // IDataProtector / IDataProtectionProvider
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;    // IdentityDbContext
using Microsoft.EntityFrameworkCore;                        // DbContext, DbSet, ModelBuilder
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // ValueConverter

namespace HealthTrack.Data
{
    /// <summary>
    /// The EF Core database context. Inherits the Identity tables
    /// (users, roles, logins) and holds all HealthTrack tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>   // Identity tables use our ApplicationUser
    {
        // Used to encrypt sensitive health text columns (see OnModelCreating)
        private readonly IDataProtector _protector;

        // Constructor: ASP.NET Core injects the options (connection string) and the Data Protection provider
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IDataProtectionProvider dataProtectionProvider)
            : base(options)                                    // pass the options on to IdentityDbContext
        {
            // Create a protector for one purpose only, so these values cannot be decrypted elsewhere
            _protector = dataProtectionProvider.CreateProtector(HealthDataEncryption.Purpose);
        }

        // Each DbSet becomes a table in SQL Server
        public DbSet<TrainerAssignment> TrainerAssignments { get; set; }   // client <-> trainer links
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }               // workout plans
        public DbSet<WorkoutPlanItem> WorkoutPlanItems { get; set; }       // exercises inside plans
        public DbSet<DietPlan> DietPlans { get; set; }                     // diet plans
        public DbSet<DietPlanItem> DietPlanItems { get; set; }             // meals inside diet plans
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }                 // workouts clients logged
        public DbSet<MealLog> MealLogs { get; set; }                       // meals clients logged
        public DbSet<HealthMeasurement> HealthMeasurements { get; set; }   // weight / height / heart rate
        public DbSet<Appointment> Appointments { get; set; }               // consultations
        public DbSet<Feedback> Feedbacks { get; set; }                     // trainer feedback
        public DbSet<Notification> Notifications { get; set; }             // bell notifications

        // Runs once when EF Core builds its picture of the database (the "model")
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Must be called first so Identity can configure its own tables
            base.OnModelCreating(builder);

            // Many tables link to ApplicationUser twice (ClientId and TrainerId).
            // SQL Server does not allow multiple cascade-delete paths, so for our
            // HealthTrack tables we use Restrict: a user with health data cannot be
            // hard-deleted; the Admin deactivates them instead (IsActive = false),
            // which also protects the client's health records.
            var userForeignKeys = builder.Model.GetEntityTypes()                        // every table EF knows about
                .Where(e => e.ClrType.Namespace == "HealthTrack.Models")                // only our own tables (not Identity's)
                .SelectMany(e => e.GetForeignKeys())                                    // all their foreign keys
                .Where(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser)); // only keys that point to a user

            foreach (var foreignKey in userForeignKeys)                                 // for each of those keys...
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;                    // ...block delete instead of cascading
            }

            // ---------------------------------------------------------------
            // SECURITY: encryption at rest for sensitive health text
            // ---------------------------------------------------------------
            // A value converter runs automatically on every save and load:
            //   C# (plain text)  --Encrypt-->  SQL Server (cipher text)
            //   SQL Server       --Decrypt-->  C# (plain text)
            // Anyone who opens the database directly only sees unreadable text.
            var protector = _protector;                                                  // local copy used by the lambdas below
            var encryptedText = new ValueConverter<string, string>(
                plain => HealthDataEncryption.Encrypt(protector, plain),                // when saving: encrypt
                stored => HealthDataEncryption.Decrypt(protector, stored));             // when loading: decrypt

            // Encrypted text is longer than the original, so the columns are widened to 2000.
            builder.Entity<HealthMeasurement>()                    // the HealthMeasurements table...
                .Property(h => h.Notes)                            // ...its Notes column
                .HasMaxLength(2000)                                // room for the longer encrypted text
                .HasConversion(encryptedText);                     // encrypt/decrypt automatically

            builder.Entity<Appointment>()                          // the Appointments table...
                .Property(a => a.TrainerNotes)                     // ...its TrainerNotes column
                .HasMaxLength(2000)                                // room for the longer encrypted text
                .HasConversion(encryptedText);                     // encrypt/decrypt automatically
        }
    }
}
