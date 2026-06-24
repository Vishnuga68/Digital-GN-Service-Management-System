using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class CitizenSpecialStatusService : ICitizenSpecialStatusService
    {
        private readonly ICitizenSpecialStatusRepository _repository;

        public CitizenSpecialStatusService(ICitizenSpecialStatusRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CitizenSpecialStatus>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<CitizenSpecialStatus> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("CitizenSpecialStatus not found.");
            }

            return entity;
        }

        public async Task<CitizenSpecialStatus> CreateAsync(CitizenSpecialStatus entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<CitizenSpecialStatus> UpdateAsync(string id, CitizenSpecialStatus entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}