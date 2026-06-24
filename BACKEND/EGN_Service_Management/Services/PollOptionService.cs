using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class PollOptionService : IPollOptionService
    {
        private readonly IPollOptionRepository _repository;

        public PollOptionService(IPollOptionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PollOption>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<PollOption> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("PollOption not found.");
            }

            return entity;
        }

        public async Task<PollOption> CreateAsync(PollOption entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<PollOption> UpdateAsync(string id, PollOption entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}