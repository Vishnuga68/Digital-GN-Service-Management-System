using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class CitizenMasterService : ICitizenMasterService
    {
        private readonly ICitizenMasterRepository _repository;

        public CitizenMasterService(ICitizenMasterRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CitizenMaster>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<CitizenMaster> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("CitizenMaster not found.");
            }

            return entity;
        }

        public async Task<CitizenMaster> CreateAsync(CitizenMaster entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<CitizenMaster> UpdateAsync(string id, CitizenMaster entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}