using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userNic, string role)
        {
            return await _repository.GetUserNotificationsAsync(userNic, role);
        }

        public async Task<int> GetUnreadCountAsync(string userNic, string role)
        {
            return await _repository.GetUnreadCountAsync(userNic, role);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            await _repository.MarkAsReadAsync(notificationId);
        }

        public async Task MarkAllAsReadAsync(string userNic, string role)
        {
            await _repository.MarkAllAsReadAsync(userNic, role);
        }

        public async Task SendNotificationAsync(
            string recipientNic,
            string recipientRole,
            string type,
            string title,
            string message,
            string? relatedEntityType = null,
            string? relatedEntityId = null,
            string? actionUrl = null,
            string priority = "Low",
            string? createdBy = null)
        {
            // Safeguard: The notification sender does not receive a notification for their own action.
            if (!string.IsNullOrEmpty(createdBy) && createdBy == recipientNic)
            {
                return;
            }

            var notification = new Notification
            {
                RecipientNic = recipientNic,
                RecipientRole = recipientRole,
                Type = type,
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                ActionUrl = actionUrl,
                Priority = priority,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy ?? "System",
                IsActive = true
            };

            await _repository.AddAsync(notification);
        }
    }
}