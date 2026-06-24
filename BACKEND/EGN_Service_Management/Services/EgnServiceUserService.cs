using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class EgnServiceUserService : IEgnServiceUserService
    {
        private readonly IEgnServiceUserRepository _repository;

        public EgnServiceUserService(IEgnServiceUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<EgnServiceUser>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<EgnServiceUser> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("EgnServiceUser not found.");
            }

            return entity;
        }

        public async Task<EgnServiceUser> CreateAsync(EgnServiceUser entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<EgnServiceUser> UpdateAsync(string id, EgnServiceUser entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
