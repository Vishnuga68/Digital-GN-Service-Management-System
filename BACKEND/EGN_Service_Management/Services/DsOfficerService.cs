using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class DsOfficerService : IDsOfficerService
    {
        private readonly IDsOfficerRepository _repository;

        public DsOfficerService(IDsOfficerRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DsOfficer>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<DsOfficer> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("DsOfficer not found.");
            }

            return entity;
        }

        public async Task<DsOfficer> CreateAsync(DsOfficer entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<DsOfficer> UpdateAsync(string id, DsOfficer entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}