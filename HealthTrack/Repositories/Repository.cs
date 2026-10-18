using HealthTrack.Data;
using HealthTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();   // the table for this entity type
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}