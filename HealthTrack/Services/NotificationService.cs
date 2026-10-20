using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;

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

        private const string LogsLink = "/Logs";

        private readonly INotificationRepository _notifications;
        private readonly IAppointmentRepository _appointments;
        private readonly IRepository<WorkoutPlan> _workoutPlans;
        private readonly IRepository<WorkoutPlanItem> _exercises;
        private readonly IRepository<DietPlan> _dietPlans;
        private readonly IRepository<DietPlanItem> _plannedMeals;
        private readonly IRepository<WorkoutLog> _workoutLogs;
        private readonly IRepository<MealLog> _mealLogs;

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
            _notifications = notifications;
            _appointments = appointments;
            _workoutPlans = workoutPlans;
            _exercises = exercises;
            _dietPlans = dietPlans;
            _plannedMeals = plannedMeals;
            _workoutLogs = workoutLogs;
            _mealLogs = mealLogs;
        }

        public async Task SendAsync(string userId, string title, string message, NotificationType type, string? link = null)
        {
            await _notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                CreatedAt = DateTime.Now
            });
            await _notifications.SaveAsync();
        }

        public async Task GenerateRemindersAsync(string userId)
        {
            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            bool created = false;

            // Reminders this user already has (used to avoid duplicates)
            var existing = (await _notifications.FindAsync(n => n.UserId == userId &&
                (n.Title == AppointmentReminderTitle || n.CreatedAt >= today))).ToList();

            // ---------- 1. Appointments in the next 24 hours (clients and trainers) ----------
            DateTime in24Hours = now.AddHours(24);
            var upcoming = await _appointments.FindAsync(a =>
                (a.ClientId == userId || a.TrainerId == userId) &&
                a.AppointmentDate >= now && a.AppointmentDate <= in24Hours &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved));

            foreach (var appointment in upcoming)
            {
                string link = $"/Appointments/Details/{appointment.Id}";
                bool alreadyReminded = existing.Any(n => n.Title == AppointmentReminderTitle && n.Link == link);
                if (alreadyReminded) continue;

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
                created = true;
            }

            // ---------- 2. Workout reminder (clients with a workout planned today) ----------
            bool workoutReminderSentToday = existing.Any(n => n.Title == WorkoutReminderTitle && n.CreatedAt >= today);
            if (!workoutReminderSentToday)
            {
                var activePlanIds = (await _workoutPlans.FindAsync(p => p.ClientId == userId &&
                        p.StartDate <= today && (p.EndDate == null || p.EndDate >= today)))
                    .Select(p => p.Id).ToList();

                if (activePlanIds.Count > 0)
                {
                    DayOfWeek dayOfWeek = today.DayOfWeek;
                    var todaysExercises = (await _exercises.FindAsync(e =>
                        activePlanIds.Contains(e.WorkoutPlanId) && e.DayOfWeek == dayOfWeek)).ToList();
                    bool loggedToday = (await _workoutLogs.FindAsync(w =>
                        w.ClientId == userId && w.Date >= today && w.Date < tomorrow)).Any();

                    if (todaysExercises.Count > 0 && !loggedToday)
                    {
                        string names = string.Join(", ", todaysExercises.Select(e => e.ExerciseName).Distinct().Take(3));
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
                        created = true;
                    }
                }
            }

            // ---------- 3. Meal reminder (clients with an active diet plan) ----------
            bool mealReminderSentToday = existing.Any(n => n.Title == MealReminderTitle && n.CreatedAt >= today);
            if (!mealReminderSentToday)
            {
                var activeDietPlans = (await _dietPlans.FindAsync(p => p.ClientId == userId &&
                    p.StartDate <= today && (p.EndDate == null || p.EndDate >= today))).ToList();

                if (activeDietPlans.Count > 0)
                {
                    var planIds = activeDietPlans.Select(p => p.Id).ToList();
                    var meals = (await _plannedMeals.FindAsync(m => planIds.Contains(m.DietPlanId))).ToList();
                    bool loggedMealToday = (await _mealLogs.FindAsync(m =>
                        m.ClientId == userId && m.Date >= today && m.Date < tomorrow)).Any();

                    if (meals.Count > 0 && !loggedMealToday)
                    {
                        // Mention the next planned meal (by time) if there is one later today
                        var nextMeal = meals
                            .Where(m => m.MealTime.HasValue && m.MealTime.Value > now.TimeOfDay)
                            .OrderBy(m => m.MealTime)
                            .FirstOrDefault();

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
                        created = true;
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
