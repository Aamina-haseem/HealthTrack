using HealthTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Data
{
    /// <summary>
    /// Creates the starting data when the application runs:
    /// roles, an admin account, sample trainers/clients and demo health records.
    /// Every step checks first, so running it again never creates duplicates.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply any pending migrations automatically (creates the database on a new PC)
            await context.Database.MigrateAsync();

            // ---------- 1. ROLES ----------
            string[] roles = { AppRoles.Admin, AppRoles.Trainer, AppRoles.Client };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ---------- 2. USERS ----------
            await CreateUserAsync(userManager, "admin@healthtrack.com", "Admin@123",
                "System", "Admin", AppRoles.Admin);

            var trainer1 = await CreateUserAsync(userManager, "trainer1@healthtrack.com", "Trainer@123",
                "Kamal", "Perera", AppRoles.Trainer, "Fitness Trainer");

            var trainer2 = await CreateUserAsync(userManager, "trainer2@healthtrack.com", "Trainer@123",
                "Nadia", "Fernando", AppRoles.Trainer, "Nutritionist");

            var client1 = await CreateUserAsync(userManager, "client1@healthtrack.com", "Client@123",
                "Amal", "Silva", AppRoles.Client);

            var client2 = await CreateUserAsync(userManager, "client2@healthtrack.com", "Client@123",
                "Sara", "Jayasinghe", AppRoles.Client);

            // ---------- 3. TRAINER ASSIGNMENTS ----------
            if (!await context.TrainerAssignments.AnyAsync())
            {
                context.TrainerAssignments.AddRange(
                    new TrainerAssignment { ClientId = client1.Id, TrainerId = trainer1.Id },
                    new TrainerAssignment { ClientId = client2.Id, TrainerId = trainer2.Id });
            }

            // ---------- 4. DEMO HEALTH DATA (so charts have something to show) ----------
            if (!await context.HealthMeasurements.AnyAsync())
            {
                double[] weights = { 82.0, 81.2, 80.5, 79.9, 79.1, 78.4 };   // steady weight loss
                for (int i = 0; i < weights.Length; i++)
                {
                    context.HealthMeasurements.Add(new HealthMeasurement
                    {
                        ClientId = client1.Id,
                        Date = DateTime.Today.AddDays(-7 * (weights.Length - 1 - i)),  // one per week
                        WeightKg = weights[i],
                        HeightCm = 175,
                        RestingHeartRate = 72 - i
                    });
                }
            }

            if (!await context.WorkoutLogs.AnyAsync())
            {
                context.WorkoutLogs.AddRange(
                    new WorkoutLog { ClientId = client1.Id, ExerciseName = "Running", Date = DateTime.Today.AddDays(-3), DurationMinutes = 30, CaloriesBurned = 300 },
                    new WorkoutLog { ClientId = client1.Id, ExerciseName = "Cycling", Date = DateTime.Today.AddDays(-2), DurationMinutes = 45, CaloriesBurned = 400 },
                    new WorkoutLog { ClientId = client1.Id, ExerciseName = "Squats", Date = DateTime.Today.AddDays(-1), DurationMinutes = 20, CaloriesBurned = 150 });
            }

            if (!await context.Appointments.AnyAsync())
            {
                context.Appointments.Add(new Appointment
                {
                    ClientId = client1.Id,
                    TrainerId = trainer1.Id,
                    AppointmentDate = DateTime.Today.AddDays(2).AddHours(10),   // in 2 days at 10:00
                    Reason = "Monthly progress review",
                    Status = AppointmentStatus.Pending
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a user with a role if the email does not exist yet; otherwise returns the existing user.
        /// </summary>
        private static async Task<ApplicationUser> CreateUserAsync(
            UserManager<ApplicationUser> userManager, string email, string password,
            string firstName, string lastName, string role, string? specialization = null)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                return existing;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                Specialization = specialization
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                // Stop with a clear message instead of failing silently
                throw new InvalidOperationException(
                    $"Could not create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, role);
            return user;
        }
    }
}