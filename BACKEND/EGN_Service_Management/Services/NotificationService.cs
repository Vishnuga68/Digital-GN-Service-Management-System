using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;

        public NotificationService(INotificationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Notification>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Notification> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Notification not found.");
            }

            return entity;
        }

        public async Task<Notification> CreateAsync(Notification entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<Notification> UpdateAsync(int id, Notification entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}