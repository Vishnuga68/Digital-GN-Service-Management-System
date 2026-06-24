using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetAllAsync();
        Task<Notification> GetByIdAsync(int id);
        Task<Notification> CreateAsync(Notification entity);
        Task<Notification> UpdateAsync(int id, Notification entity);
        Task DeleteAsync(int id);
    }
}