// =====================================================================
// FILE:    Models/AppRoles.cs
// PURPOSE: Holds the three role names used everywhere in the app (Admin, Trainer,
//          Client). Controllers use them in [Authorize(Roles = AppRoles.Trainer)]
//          and the seeder uses them to create the roles.
// KEY IDEA: constants instead of typed strings, so a spelling mistake becomes a
//           compile error instead of a security hole.
// =====================================================================

namespace HealthTrack.Models   // all model classes live in this namespace
{
    /// <summary>
    /// Central list of role names. Using constants instead of typing "Client" etc.
    /// everywhere prevents spelling mistakes in [Authorize(Roles = ...)].
    /// </summary>
    public static class AppRoles   // static: never instantiated, only holds constants
    {
        public const string Admin = "Admin";       // system administrator: manages users and assignments
        public const string Trainer = "Trainer";   // trainer / health professional: creates plans, gives feedback
        public const string Client = "Client";     // client: follows plans and logs activity
    }
}