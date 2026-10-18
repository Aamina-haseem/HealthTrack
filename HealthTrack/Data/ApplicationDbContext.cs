using HealthTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Data
{
    /// <summary>
    /// The EF Core database context. Inherits the Identity tables
    /// (users, roles, logins) and will hold all HealthTrack tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for HealthTrack entities will be added in Step 6
    }
}