// =====================================================================
// FILE:    Data/DbSeeder.cs
// PURPOSE: Runs every time the app starts (called from Program.cs). It applies
//          any pending EF Core migrations (which creates the database on a new
//          PC), then adds the starting data: the three roles, an admin account,
//          two trainers, two clients, their trainer assignments and some demo
//          health records so the charts have something to show.
// KEY IDEAS: idempotent seeding (every step checks first, so running it again
//            never creates duplicates), UserManager / RoleManager from Identity.
// TEST ACCOUNTS: the emails and passwords are listed in the CreateUserAsync calls below.
// =====================================================================

using HealthTrack.Models;                 // ApplicationUser, AppRoles and the model classes
using Microsoft.AspNetCore.Identity;      // UserManager, RoleManager, IdentityRole
using Microsoft.EntityFrameworkCore;      // MigrateAsync, AnyAsync

namespace HealthTrack.Data
{
    /// <summary>
    /// Creates the starting data when the application runs:
    /// roles, an admin account, sample trainers/clients and demo health records.
    /// Every step checks first, so running it again never creates duplicates.
    /// </summary>
    public static class DbSeeder
    {
        // Entry point called from Program.cs with the app's services
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();                    // the database
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();        // creates users (hashes passwords)
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();           // creates roles

            // Apply any pending migrations automatically (creates the database on a new PC)
            await context.Database.MigrateAsync();

            // ---------- 1. ROLES ----------
            string[] roles = { AppRoles.Admin, AppRoles.Trainer, AppRoles.Client };             // the three roles
            foreach (var role in roles)                                                          // for each role...
            {
                if (!await roleManager.RoleExistsAsync(role))                                    // ...if it is not there yet
                {
                    await roleManager.CreateAsync(new IdentityRole(role));                       // ...create it
                }
            }

            // ---------- 2. USERS ----------
            // The administrator account
            await CreateUserAsync(userManager, "admin@healthtrack.com", "Admin@123",
                "System", "Admin", AppRoles.Admin);

            // Trainer 1 (fitness trainer), assigned to client 1 below
            var trainer1 = await CreateUserAsync(userManager, "trainer1@healthtrack.com", "Trainer@123",
                "Kamal", "Perera", AppRoles.Trainer, "Fitness Trainer");

            // Trainer 2 (nutritionist), assigned to client 2 below
            var trainer2 = await CreateUserAsync(userManager, "trainer2@healthtrack.com", "Trainer@123",
                "Nadia", "Fernando", AppRoles.Trainer, "Nutritionist");

            // Client 1: gets the demo health data, so their charts are filled
            var client1 = await CreateUserAsync(userManager, "client1@healthtrack.com", "Client@123",
                "Amal", "Silva", AppRoles.Client);

            // Client 2: starts with no data (useful to show the empty states)
            var client2 = await CreateUserAsync(userManager, "client2@healthtrack.com", "Client@123",
                "Sara", "Jayasinghe", AppRoles.Client);

            // ---------- 3. TRAINER ASSIGNMENTS ----------
            if (!await context.TrainerAssignments.AnyAsync())                                    // only if there are none yet
            {
                context.TrainerAssignments.AddRange(
                    new TrainerAssignment { ClientId = client1.Id, TrainerId = trainer1.Id },   // Amal -> Kamal
                    new TrainerAssignment { ClientId = client2.Id, TrainerId = trainer2.Id });  // Sara -> Nadia
            }

            // ---------- 4. DEMO HEALTH DATA (so charts have something to show) ----------
            if (!await context.HealthMeasurements.AnyAsync())                                   // only if there are none yet
            {
                double[] weights = { 82.0, 81.2, 80.5, 79.9, 79.1, 78.4 };   // steady weight loss
                for (int i = 0; i < weights.Length; i++)                                         // one measurement per weight
                {
                    context.HealthMeasurements.Add(new HealthMeasurement
                    {
                        ClientId = client1.Id,                                                   // all for client 1
                        Date = DateTime.Today.AddDays(-7 * (weights.Length - 1 - i)),  // one per week
                        WeightKg = weights[i],                                                   // the weight for that week
                        HeightCm = 175,                                                          // same height every time
                        RestingHeartRate = 72 - i                                                // heart rate slowly improves
                    });
                }
            }

            if (!await context.WorkoutLogs.AnyAsync())                                          // only if there are none yet
            {
                // Three workouts on the last three days
                context.WorkoutLogs.AddRange(
                    new WorkoutLog { ClientId = client1.Id, ExerciseName = "Running", Date = DateTime.Today.AddDays(-3), DurationMinutes = 30, CaloriesBurned = 300 },
                    new WorkoutLog { ClientId = client1.Id, ExerciseName = "Cycling", Date = DateTime.Today.AddDays(-2), DurationMinutes = 45, CaloriesBurned = 400 },
                    new WorkoutLog { ClientId = client1.Id, ExerciseName = "Squats", Date = DateTime.Today.AddDays(-1), DurationMinutes = 20, CaloriesBurned = 150 });
            }

            if (!await context.Appointments.AnyAsync())                                         // only if there are none yet
            {
                context.Appointments.Add(new Appointment
                {
                    ClientId = client1.Id,                                                       // booked by client 1...
                    TrainerId = trainer1.Id,                                                     // ...with trainer 1
                    AppointmentDate = DateTime.Today.AddDays(2).AddHours(10),   // in 2 days at 10:00
                    Reason = "Monthly progress review",                                          // why it was booked
                    Status = AppointmentStatus.Pending                                           // waits for the trainer's approval
                });
            }

            await context.SaveChangesAsync();                                                    // save everything added above in one go
        }

        /// <summary>
        /// Creates a user with a role if the email does not exist yet; otherwise returns the existing user.
        /// </summary>
        private static async Task<ApplicationUser> CreateUserAsync(
            UserManager<ApplicationUser> userManager, string email, string password,
            string firstName, string lastName, string role, string? specialization = null)
        {
            var existing = await userManager.FindByEmailAsync(email);       // is this email already registered?
            if (existing != null)
            {
                return existing;                                             // yes: reuse it (no duplicates)
            }

            // Build the new user object
            var user = new ApplicationUser
            {
                UserName = email,                                            // Identity signs in with the user name, so use the email
                Email = email,
                EmailConfirmed = true,                                       // no email server, so mark it as confirmed
                FirstName = firstName,
                LastName = lastName,
                Specialization = specialization                              // only set for trainers
            };

            var result = await userManager.CreateAsync(user, password);     // saves the user with a hashed password
            if (!result.Succeeded)                                           // e.g. the password breaks the policy
            {
                // Stop with a clear message instead of failing silently
                throw new InvalidOperationException(
                    $"Could not create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, role);                    // give the user their role
            return user;                                                     // hand the new user back to SeedAsync
        }
    }
}