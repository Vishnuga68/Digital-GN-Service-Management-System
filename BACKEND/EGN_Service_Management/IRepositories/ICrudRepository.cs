namespace EGN_Service_Management.IRepositories
{
    public interface ICrudRepository<TEntity, TKey> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(TKey id);
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TKey id, TEntity entity);
        Task DeleteAsync(TKey id);
        Task<bool> ExistsAsync(TKey id);
    }
}
