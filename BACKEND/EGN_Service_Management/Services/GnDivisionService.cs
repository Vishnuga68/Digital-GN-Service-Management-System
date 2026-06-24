using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class GnDivisionService : IGnDivisionService
    {
        private readonly IGnDivisionRepository _repository;

        public GnDivisionService(IGnDivisionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<GnDivision>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<GnDivision> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("GnDivision not found.");
            }

            return entity;
        }

        public async Task<GnDivision> CreateAsync(GnDivision entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<GnDivision> UpdateAsync(string id, GnDivision entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}