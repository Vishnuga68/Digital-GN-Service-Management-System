namespace EGN_Service_Management.IServices
{
    public interface ICrudService<TEntity, TKey> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity> GetByIdAsync(TKey id);
        Task<TEntity> CreateAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TKey id, TEntity entity);
        Task DeleteAsync(TKey id);
    }
}
