// =====================================================================
// FILE:    Repositories/Interfaces/IRepository.cs
// PURPOSE: The generic repository "contract": the basic Create / Read / Update /
//          Delete operations every table needs. Controllers depend on this
//          interface instead of the database, and Program.cs maps it to the
//          Repository<T> class through dependency injection.
// KEY IDEAS: Repository pattern, generics (<T>), interface-based design,
//            async/await for database calls.
// =====================================================================

using System.Linq.Expressions;   // Expression<Func<T, bool>> (a condition EF Core can turn into SQL)

namespace HealthTrack.Repositories.Interfaces
{
    /// <summary>
    /// Generic repository contract: the basic CRUD operations every entity needs.
    /// Controllers depend on this interface, not on the database directly,
    /// which keeps data-access code in one place and makes the code easier to test.
    /// </summary>
    /// <typeparam name="T">Any entity class, e.g. Appointment or WorkoutPlan</typeparam>
    public interface IRepository<T> where T : class   // T must be a class (an entity)
    {
        Task<IEnumerable<T>> GetAllAsync();           // every row in the table

        Task<T?> GetByIdAsync(int id);                // one row by primary key, or null if not found

        // Returns all records matching a condition, e.g. a => a.ClientId == userId
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);                      // mark a new row to be inserted

        void Update(T entity);                        // mark an existing row as changed

        void Delete(T entity);                        // mark a row to be deleted

        // Saves all pending changes to the database in one transaction
        Task SaveAsync();
    }
}