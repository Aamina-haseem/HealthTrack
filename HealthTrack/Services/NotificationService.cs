// =====================================================================
// FILE:    Services/NotificationService.cs
// PURPOSE: Creates notifications (Task 8):
//            - SendAsync: one notification for one user (used by the controllers)
//            - GenerateRemindersAsync: automatic reminders, checked each time the
//              bell loads: appointments in the next 24 hours, today's planned
//              workout (if not logged yet) and today's meals (if none logged yet).
//          Each reminder is created at most once (appointment reminders once per
//          appointment, workout/meal reminders once per day).
// KEY IDEAS: service layer, dependency injection of repositories, duplicate checks.
// =====================================================================

using HealthTrack.Models;                  // Notification, NotificationType, plans, logs, ...
using HealthTrack.Repositories.Interfaces; // the repository interfaces

namespace HealthTrack.Services
{
    /// <summary>
    /// Implementation of INotificationService. All data access goes through repositories
    /// (Repository pattern), so this class contains only the reminder rules.
    /// </summary>
    public class NotificationService : INotificationService
    {
        // Fixed titles are also used to detect reminders that were already sent
        public const string AppointmentReminderTitle = "Appointment reminder";
        public const string WorkoutReminderTitle = "Workout reminder";
        public const string MealReminderTitle = "Meal reminder";

        private const string LogsLink = "/Logs";                       // workout/meal reminders open the Log Activity page

        // Repositories this service reads from and writes to (injected below)
        private readonly INotificationRepository _notifications;       // notifications table
        private readonly IAppointmentRepository _appointments;         // appointments table
        private readonly IRepository<WorkoutPlan> _workoutPlans;       // workout plans
        private readonly IRepository<WorkoutPlanItem> _exercises;      // exercises inside plans
        private readonly IRepository<DietPlan> _dietPlans;             // diet plans
        private readonly IRepository<DietPlanItem> _plannedMeals;      // meals inside diet plans
        private readonly IRepository<WorkoutLog> _workoutLogs;         // workouts the client logged
        private readonly IRepository<MealLog> _mealLogs;               // meals the client logged

        // Constructor: ASP.NET Core passes in every repository (dependency injection)
        public NotificationService(
            INotificationRepository notifications,
            IAppointmentRepository appointments,
            IRepository<WorkoutPlan> workoutPlans,
            IRepository<WorkoutPlanItem> exercises,
            IRepository<DietPlan> dietPlans,
            IRepository<DietPlanItem> plannedMeals,
            IRepository<WorkoutLog> workoutLogs,
            IRepository<MealLog> mealLogs)
        {
            _notifications = notifications;   // keep each one in a field for later use
            _appointments = appointments;
            _workoutPlans = workoutPlans;
            _exercises = exercises;
            _dietPlans = dietPlans;
            _plannedMeals = plannedMeals;
            _workoutLogs = workoutLogs;
            _mealLogs = mealLogs;
        }

        // Saves one notification straight away
        public async Task SendAsync(string userId, string title, string message, NotificationType type, string? link = null)
        {
            await _notifications.AddAsync(new Notification
            {
                UserId = userId,          // who receives it
                Title = title,            // short heading
                Message = message,        // full text
                Type = type,              // category (Appointment, Feedback, ...)
                Link = link,              // optional page to open
                CreatedAt = DateTime.Now  // time stamp
            });
            await _notifications.SaveAsync();   // write it to the database
        }

        // Creates any reminders the user should have but does not have yet
        public async Task GenerateRemindersAsync(string userId)
        {
            DateTime now = DateTime.Now;              // current date and time
            DateTime today = DateTime.Today;          // today at 00:00
            DateTime tomorrow = today.AddDays(1);     // tomorrow at 00:00 (end of "today")
            bool created = false;                     // becomes true if any reminder is added

            // Reminders this user already has (used to avoid duplicates)
            var existing = (await _notifications.FindAsync(n => n.UserId == userId &&
                (n.Title == AppointmentReminderTitle || n.CreatedAt >= today))).ToList();

            // ---------- 1. Appointments in the next 24 hours (clients and trainers) ----------
            DateTime in24Hours = now.AddHours(24);                                                  // the end of the window
            var upcoming = await _appointments.FindAsync(a =>
                (a.ClientId == userId || a.TrainerId == userId) &&                                  // the user is either side
                a.AppointmentDate >= now && a.AppointmentDate <= in24Hours &&                       // within the next 24 hours
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved)); // still going ahead

            foreach (var appointment in upcoming)                                                   // for each such appointment...
            {
                string link = $"/Appointments/Details/{appointment.Id}";                            // its details page
                bool alreadyReminded = existing.Any(n => n.Title == AppointmentReminderTitle && n.Link == link);   // reminded before?
                if (alreadyReminded) continue;                                                      // yes: skip it

                await _notifications.AddAsync(new Notification
                {
                    UserId = userId,
                    Title = AppointmentReminderTitle,
                    Message = $"You have an appointment on {appointment.AppointmentDate:ddd dd MMM 'at' HH:mm} " +
                              $"(status: {appointment.Status}). Reason: {appointment.Reason}",
                    Type = NotificationType.Appointment,
                    Link = link,
                    CreatedAt = now
                });
                created = true;                                                                     // something to save
            }

            // ---------- 2. Workout reminder (clients with a workout planned today) ----------
            bool workoutReminderSentToday = existing.Any(n => n.Title == WorkoutReminderTitle && n.CreatedAt >= today);
            if (!workoutReminderSentToday)                                                          // at most one per day
            {
                // Ids of the client's plans that are running today
                var activePlanIds = (await _workoutPlans.FindAsync(p => p.ClientId == userId &&
                        p.StartDate <= today && (p.EndDate == null || p.EndDate >= today)))
                    .Select(p => p.Id).ToList();

                if (activePlanIds.Count > 0)                                                        // the client has an active plan
                {
                    DayOfWeek dayOfWeek = today.DayOfWeek;                                          // e.g. Monday
                    var todaysExercises = (await _exercises.FindAsync(e =>
                        activePlanIds.Contains(e.WorkoutPlanId) && e.DayOfWeek == dayOfWeek)).ToList();   // exercises planned for today
                    bool loggedToday = (await _workoutLogs.FindAsync(w =>
                        w.ClientId == userId && w.Date >= today && w.Date < tomorrow)).Any();       // already logged a workout today?

                    if (todaysExercises.Count > 0 && !loggedToday)                                  // planned but not done yet
                    {
                        string names = string.Join(", ", todaysExercises.Select(e => e.ExerciseName).Distinct().Take(3));   // up to 3 names
                        await _notifications.AddAsync(new Notification
                        {
                            UserId = userId,
                            Title = WorkoutReminderTitle,
                            Message = $"You have {todaysExercises.Count} exercise(s) planned for today ({names}). " +
                                      "Don't forget to log your workout.",
                            Type = NotificationType.Workout,
                            Link = LogsLink,
                            CreatedAt = now
                        });
                        created = true;                                                             // something to save
                    }
                }
            }

            // ---------- 3. Meal reminder (clients with an active diet plan) ----------
            bool mealReminderSentToday = existing.Any(n => n.Title == MealReminderTitle && n.CreatedAt >= today);
            if (!mealReminderSentToday)                                                             // at most one per day
            {
                // The client's diet plans that are running today
                var activeDietPlans = (await _dietPlans.FindAsync(p => p.ClientId == userId &&
                    p.StartDate <= today && (p.EndDate == null || p.EndDate >= today))).ToList();

                if (activeDietPlans.Count > 0)                                                      // the client has an active diet plan
                {
                    var planIds = activeDietPlans.Select(p => p.Id).ToList();                       // their ids
                    var meals = (await _plannedMeals.FindAsync(m => planIds.Contains(m.DietPlanId))).ToList();   // all planned meals
                    bool loggedMealToday = (await _mealLogs.FindAsync(m =>
                        m.ClientId == userId && m.Date >= today && m.Date < tomorrow)).Any();       // already logged a meal today?

                    if (meals.Count > 0 && !loggedMealToday)                                        // planned but nothing logged yet
                    {
                        // Mention the next planned meal (by time) if there is one later today
                        var nextMeal = meals
                            .Where(m => m.MealTime.HasValue && m.MealTime.Value > now.TimeOfDay)    // meals later than now
                            .OrderBy(m => m.MealTime)                                               // earliest first
                            .FirstOrDefault();                                                      // the next one, or null

                        // Use the next meal if there is one, otherwise a general message
                        string message = nextMeal != null
                            ? $"Next meal: {nextMeal.MealType} at {nextMeal.MealTime!.Value:hh\\:mm} - {nextMeal.FoodItems}. Remember to log what you eat."
                            : $"Your diet plan has {meals.Count} planned meal(s) (target {activeDietPlans[0].DailyCalorieTarget} kcal). Remember to log what you eat.";

                        await _notifications.AddAsync(new Notification
                        {
                            UserId = userId,
                            Title = MealReminderTitle,
                            Message = message,
                            Type = NotificationType.Meal,
                            Link = LogsLink,
                            CreatedAt = now
                        });
                        created = true;                                                             // something to save
                    }
                }
            }

            // Save all new reminders in one database call
            if (created)
            {
                await _notifications.SaveAsync();
            }
        }
    }
}
