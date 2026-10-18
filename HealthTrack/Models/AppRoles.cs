namespace HealthTrack.Models
{
    /// <summary>
    /// Central list of role names. Using constants instead of typing "Client" etc.
    /// everywhere prevents spelling mistakes in [Authorize(Roles = ...)].
    /// </summary>
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Trainer = "Trainer";
        public const string Client = "Client";
    }
}