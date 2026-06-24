using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class FamilyCardService : IFamilyCardService
    {
        private readonly IFamilyCardRepository _repository;

        public FamilyCardService(IFamilyCardRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<FamilyCard>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<FamilyCard> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("FamilyCard not found.");
            }

            return entity;
        }

        public async Task<FamilyCard> CreateAsync(FamilyCard entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<FamilyCard> UpdateAsync(string id, FamilyCard entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}