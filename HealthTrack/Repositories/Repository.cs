// =====================================================================
// FILE:    Repositories/Repository.cs
// PURPOSE: The generic repository class: one implementation of the basic CRUD
//          operations (IRepository<T>) that works for ANY table, using EF Core.
//          The specific repositories (WorkoutPlanRepository, ...) inherit from it
//          and only add their own extra queries.
// KEY IDEAS: generics, inheritance, "protected" fields shared with child classes,
//            EF Core change tracking (Add/Update/Remove only mark rows; SaveAsync writes them).
// =====================================================================

using HealthTrack.Data;                    // ApplicationDbContext
using HealthTrack.Repositories.Interfaces; // IRepository<T>
using Microsoft.EntityFrameworkCore;       // DbSet, ToListAsync, FindAsync
using System.Linq.Expressions;             // Expression<Func<T, bool>>

namespace HealthTrack.Repositories
{
    /// <summary>
    /// Generic implementation of IRepository using Entity Framework Core.
    /// Specific repositories (e.g. AppointmentRepository) inherit from this
    /// and only add the extra queries they need.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        // protected so child repositories can write their own queries
        protected readonly ApplicationDbContext _context;   // the database context (one per web request)
        protected readonly DbSet<T> _dbSet;                 // the table for type T

        // The DbContext is injected by ASP.NET Core
        public Repository(ApplicationDbContext context)
        {
            _context = context;          // keep the context for SaveAsync
            _dbSet = context.Set<T>();   // the table for this entity type
        }

        // virtual = a child repository may replace it with its own version
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();               // SELECT * FROM the table
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);               // look up by primary key (null if not found)
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();   // EF Core turns the condition into a SQL WHERE
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);                   // marked as "Added"; inserted on SaveAsync
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);                           // marked as "Modified"; updated on SaveAsync
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);                           // marked as "Deleted"; deleted on SaveAsync
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();               // writes every pending change in one transaction
        }
    }
}