using System.Collections.Generic;
using System.Threading.Tasks;
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
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userNic, string role);
        Task<int> GetUnreadCountAsync(string userNic, string role);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userNic, string role);
        Task SendNotificationAsync(string recipientNic, string recipientRole, string type, string title, string message, string? relatedEntityType = null, string? relatedEntityId = null, string? actionUrl = null, string priority = "Low", string? createdBy = null);
    }
}