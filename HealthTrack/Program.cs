using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories;
using HealthTrack.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// 1. DATABASE
// ---------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------------------------------------------------------------
// 2. IDENTITY (login, registration, roles)
// ---------------------------------------------------------------
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;      // no email server in this project
        options.Password.RequiredLength = 8;                 // stronger passwords
        options.Lockout.MaxFailedAccessAttempts = 5;         // lock account after 5 wrong attempts
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()                                // enables Client / Trainer / Admin roles
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ---------------------------------------------------------------
// 3. MVC
// ---------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------
// 4. REPOSITORIES (Repository pattern + dependency injection)
// ---------------------------------------------------------------
// Generic repository: any controller can ask for IRepository<SomeEntity>
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Specific repositories (each adds its own queries on top of the generic CRUD)
builder.Services.AddScoped<ITrainerAssignmentRepository, TrainerAssignmentRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IWorkoutPlanRepository, WorkoutPlanRepository>();
builder.Services.AddScoped<IDietPlanRepository, DietPlanRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();
builder.Services.AddScoped<IMealLogRepository, MealLogRepository>();
builder.Services.AddScoped<IHealthMeasurementRepository, HealthMeasurementRepository>();

var app = builder.Build();

// ---------------------------------------------------------------
// 5. HTTP REQUEST PIPELINE
// ---------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();             // forces browsers to use HTTPS
}

app.UseHttpsRedirection();     // redirects http:// to https:// (encrypted connection)
app.UseRouting();

app.UseAuthentication();       // who are you? (must come before authorization)
app.UseAuthorization();        // what are you allowed to do?

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()            // Identity login/register pages
   .WithStaticAssets();

app.Run();