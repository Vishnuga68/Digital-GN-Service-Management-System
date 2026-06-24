using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class CrudService<TEntity, TKey> : ICrudService<TEntity, TKey> where TEntity : class
    {
        private readonly ICrudRepository<TEntity, TKey> _repository;

        public CrudService(ICrudRepository<TEntity, TKey> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<TEntity> GetByIdAsync(TKey id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"{typeof(TEntity).Name} not found.");
            }

            return entity;
        }

        public async Task<TEntity> CreateAsync(TEntity entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<TEntity> UpdateAsync(TKey id, TEntity entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(TKey id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
