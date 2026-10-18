using System.Linq.Expressions;

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Generic repository contract: the basic CRUD operations every entity needs.
    /// Controllers depend on this interface, not on the database directly,
    /// which keeps data-access code in one place and makes the code easier to test.
    /// </summary>
    /// <typeparam name="T">Any entity class, e.g. Appointment or WorkoutPlan</typeparam>
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();

        Task<T?> GetByIdAsync(int id);

        // Returns all records matching a condition, e.g. a => a.ClientId == userId
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);

        void Update(T entity);

        void Delete(T entity);

        // Saves all pending changes to the database in one transaction
        Task SaveAsync();
    }
}