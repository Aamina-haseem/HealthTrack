namespace HealthTrack.Models
{
    // Fixed sets of values used by several models (stored as numbers in the database)

    public enum AppointmentStatus { Pending, Approved, Cancelled, Completed }

    public enum MealType { Breakfast, Lunch, Dinner, Snack }

    public enum DifficultyLevel { Beginner, Intermediate, Advanced }

    public enum NotificationType { Workout, Meal, Appointment, Feedback, System }
}