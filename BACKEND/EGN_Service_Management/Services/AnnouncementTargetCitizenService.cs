using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class AnnouncementTargetCitizenService : IAnnouncementTargetCitizenService
    {
        private readonly IAnnouncementTargetCitizenRepository _repository;

        public AnnouncementTargetCitizenService(IAnnouncementTargetCitizenRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<AnnouncementTargetCitizen>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<AnnouncementTargetCitizen> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("AnnouncementTargetCitizen not found.");
            }

            return entity;
        }

        public async Task<AnnouncementTargetCitizen> CreateAsync(AnnouncementTargetCitizen entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<AnnouncementTargetCitizen> UpdateAsync(int id, AnnouncementTargetCitizen entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}