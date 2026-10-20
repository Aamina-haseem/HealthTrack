using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.Repositories;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

// Data Protection API: provides the keys used to encrypt sensitive health notes
builder.Services.AddDataProtection()
    .SetApplicationName("HealthTrack");

// ---------------------------------------------------------------
// 2. IDENTITY (login, registration, roles) + SECURITY SETTINGS
// ---------------------------------------------------------------
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;      // no email server in this project

        // Password policy: 8+ characters with upper, lower, digit and symbol
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;

        // Brute-force protection: lock the account for 5 minutes after 5 wrong passwords
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;

        // One account per email address
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()                                // enables Client / Trainer / Admin roles
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Login cookie: not readable by JavaScript, only sent over HTTPS, expires after 60 idle minutes
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// When the Admin deactivates a user, their login is checked again within 1 minute
// and they are signed out automatically (see AdminController.ToggleActive).
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);
});

// ---------------------------------------------------------------
// 3. MVC
// ---------------------------------------------------------------
// AutoValidateAntiforgeryToken: every POST form must carry an anti-forgery token,
// which protects against Cross-Site Request Forgery (CSRF) attacks.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

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

// ---------------------------------------------------------------
// 5. SERVICES (business logic shared by several controllers)
// ---------------------------------------------------------------
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// ---------------------------------------------------------------
// 6. SEED DATA (roles, test accounts, demo records)
// ---------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// ---------------------------------------------------------------
// 7. HTTP REQUEST PIPELINE
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

// Security headers sent with every response
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";                    // stops MIME-type sniffing
    context.Response.Headers["X-Frame-Options"] = "DENY";                              // stops clickjacking in iframes
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";   // limits leaked URLs
    await next();
});

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
