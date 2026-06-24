using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class GnOfficerService : IGnOfficerService
    {
        private readonly IGnOfficerRepository _repository;

        public GnOfficerService(IGnOfficerRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<GnOfficer>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<GnOfficer> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("GnOfficer not found.");
            }

            return entity;
        }

        public async Task<GnOfficer> CreateAsync(GnOfficer entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<GnOfficer> UpdateAsync(string id, GnOfficer entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}