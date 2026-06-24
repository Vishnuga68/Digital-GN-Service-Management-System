using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class DsDivisionService : IDsDivisionService
    {
        private readonly IDsDivisionRepository _repository;

        public DsDivisionService(IDsDivisionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DsDivision>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<DsDivision> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("DsDivision not found.");
            }

            return entity;
        }

        public async Task<DsDivision> CreateAsync(DsDivision entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<DsDivision> UpdateAsync(string id, DsDivision entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}