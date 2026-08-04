using System.Collections.Generic;
using System.Threading.Tasks;
using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetAllAsync();
        Task<Notification?> GetByIdAsync(int id);
        Task<Notification> AddAsync(Notification entity);
        Task<Notification> UpdateAsync(int id, Notification entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userNic, string role);
        Task<int> GetUnreadCountAsync(string userNic, string role);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userNic, string role);
    }
}