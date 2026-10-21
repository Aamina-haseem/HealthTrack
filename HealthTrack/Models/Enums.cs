// =====================================================================
// FILE:    Models/Enums.cs
// PURPOSE: The fixed lists of values used by several models: appointment
//          statuses, meal types, workout difficulty levels and notification
//          types. EF Core stores each value as a number (0, 1, 2, ...).
// KEY IDEA: an enum means only valid values can ever be saved.
// =====================================================================

namespace HealthTrack.Models
{
    // Fixed sets of values used by several models (stored as numbers in the database)

    public enum AppointmentStatus { Pending, Approved, Cancelled, Completed }   // life cycle of an appointment

    public enum MealType { Breakfast, Lunch, Dinner, Snack }                    // used by diet plans and meal logs

    public enum DifficultyLevel { Beginner, Intermediate, Advanced }            // level of a workout plan

    public enum NotificationType { Workout, Meal, Appointment, Feedback, System } // decides the bell icon/category
}