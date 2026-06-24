using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IAnnouncementRepository _repository;

        public AnnouncementService(IAnnouncementRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Announcement>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Announcement> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Announcement not found.");
            }

            return entity;
        }

        public async Task<Announcement> CreateAsync(Announcement entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<Announcement> UpdateAsync(string id, Announcement entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}