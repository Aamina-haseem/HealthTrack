// =====================================================================
// FILE:    Program.cs
// PURPOSE: The start-up file of the whole application. It runs once when the
//          app starts and:
//            1. registers every service the app needs (database, login system,
//               MVC, repositories, notification service) for dependency injection
//            2. fills the database with roles, test accounts and demo data
//            3. sets the order in which every web request is handled (the pipeline)
// KEY IDEAS: dependency injection, middleware pipeline, ASP.NET Core Identity,
//            security settings (password policy, lockout, secure cookie, CSRF, headers)
// =====================================================================

using HealthTrack.Data;                         // ApplicationDbContext and DbSeeder
using HealthTrack.Models;                       // ApplicationUser (our user class)
using HealthTrack.Repositories;                 // the repository classes
using HealthTrack.Repositories.Interfaces;      // the repository interfaces
using HealthTrack.Services;                     // NotificationService
using Microsoft.AspNetCore.DataProtection;      // encryption keys for sensitive notes
using Microsoft.AspNetCore.Identity;            // login, users and roles
using Microsoft.AspNetCore.Mvc;                 // AutoValidateAntiforgeryTokenAttribute
using Microsoft.EntityFrameworkCore;            // UseSqlServer

// Create the builder that collects all settings and services before the app is built
var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// 1. DATABASE
// ---------------------------------------------------------------
// Read the connection string from appsettings.json; stop with a clear error if it is missing
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register the EF Core database context and tell it to use SQL Server (LocalDB)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// In development, show a helpful page for database errors (e.g. missing migrations)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Data Protection API: provides the keys used to encrypt sensitive health notes
builder.Services.AddDataProtection()
    .SetApplicationName("HealthTrack");          // keys are shared by everything named "HealthTrack"

// ---------------------------------------------------------------
// 2. IDENTITY (login, registration, roles) + SECURITY SETTINGS
// ---------------------------------------------------------------
// Add ASP.NET Core Identity using our own ApplicationUser class
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;      // no email server in this project

        // Password policy: 8+ characters with upper, lower, digit and symbol
        options.Password.RequiredLength = 8;                 // at least 8 characters
        options.Password.RequireUppercase = true;            // at least one A-Z
        options.Password.RequireLowercase = true;            // at least one a-z
        options.Password.RequireDigit = true;                // at least one 0-9
        options.Password.RequireNonAlphanumeric = true;      // at least one symbol, e.g. @ or !

        // Brute-force protection: lock the account for 5 minutes after 5 wrong passwords
        options.Lockout.MaxFailedAccessAttempts = 5;                    // 5 wrong tries allowed
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // then locked for 5 minutes
        options.Lockout.AllowedForNewUsers = true;                      // also applies to new accounts

        // One account per email address
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()                                // enables Client / Trainer / Admin roles
    .AddEntityFrameworkStores<ApplicationDbContext>();      // users and roles are saved through EF Core

// Login cookie: not readable by JavaScript, only sent over HTTPS, expires after 60 idle minutes
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;                          // JavaScript cannot read the cookie (stops XSS theft)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // only sent over HTTPS (needs the https profile)
    options.Cookie.SameSite = SameSiteMode.Lax;              // not sent with most cross-site requests
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);       // signed out after 60 minutes...
    options.SlidingExpiration = true;                        // ...of inactivity (each visit resets the timer)
    options.LoginPath = "/Identity/Account/Login";           // where to send users who are not signed in
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // where to send users without the right role
});

// When the Admin deactivates a user, their login is checked again within 1 minute
// and they are signed out automatically (see AdminController.ToggleActive).
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);   // re-check the user's security stamp every minute
});

// ---------------------------------------------------------------
// 3. MVC
// ---------------------------------------------------------------
// AutoValidateAntiforgeryToken: every POST form must carry an anti-forgery token,
// which protects against Cross-Site Request Forgery (CSRF) attacks.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // applies to every controller
});

// ---------------------------------------------------------------
// 4. REPOSITORIES (Repository pattern + dependency injection)
// ---------------------------------------------------------------
// Generic repository: any controller can ask for IRepository<SomeEntity>
// AddScoped = one instance per web request (the same lifetime as the DbContext)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Specific repositories (each adds its own queries on top of the generic CRUD)
builder.Services.AddScoped<ITrainerAssignmentRepository, TrainerAssignmentRepository>(); // trainer <-> client links
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();             // appointments
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();           // notifications
builder.Services.AddScoped<IWorkoutPlanRepository, WorkoutPlanRepository>();             // workout plans
builder.Services.AddScoped<IDietPlanRepository, DietPlanRepository>();                   // diet plans
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();                   // trainer feedback
builder.Services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();               // workout logs
builder.Services.AddScoped<IMealLogRepository, MealLogRepository>();                     // meal logs
builder.Services.AddScoped<IHealthMeasurementRepository, HealthMeasurementRepository>(); // weight/height/BMI

// ---------------------------------------------------------------
// 5. SERVICES (business logic shared by several controllers)
// ---------------------------------------------------------------
builder.Services.AddScoped<INotificationService, NotificationService>(); // creates notifications

// Build the app from everything registered above
var app = builder.Build();

// ---------------------------------------------------------------
// 6. SEED DATA (roles, test accounts, demo records)
// ---------------------------------------------------------------
// Services like the DbContext are "scoped", so a scope is created just for seeding
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);         // applies migrations, then adds starting data
}

// ---------------------------------------------------------------
// 7. HTTP REQUEST PIPELINE
// ---------------------------------------------------------------
// Each app.Use... line below is a "middleware": every request passes through them in this order
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();                             // development only: lets you apply migrations from the error page
}
else
{
    app.UseExceptionHandler("/Home/Error");                 // production: friendly error page, no stack traces
    app.UseHsts();             // forces browsers to use HTTPS
}

app.UseHttpsRedirection();     // redirects http:// to https:// (encrypted connection)

// Security headers sent with every response
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";                    // stops MIME-type sniffing
    context.Response.Headers["X-Frame-Options"] = "DENY";                              // stops clickjacking in iframes
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";   // limits leaked URLs
    await next();                                                                      // pass the request on
});

app.UseRouting();              // works out which controller/action or page the URL points to

app.UseAuthentication();       // who are you? (must come before authorization)
app.UseAuthorization();        // what are you allowed to do?

app.MapStaticAssets();         // serves CSS, JavaScript and images from wwwroot

// Default MVC route: /Controller/Action/optional-id, e.g. /WorkoutPlans/Details/3
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")       // no URL = HomeController.Index
    .WithStaticAssets();

app.MapRazorPages()            // Identity login/register pages
   .WithStaticAssets();

app.Run();                     // start the web server and wait for requests
