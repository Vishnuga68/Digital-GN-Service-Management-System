using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<Notification> _dbSet;

        public NotificationRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<Notification>();
        }

        public async Task<IEnumerable<Notification>> GetAllAsync()
        {
            return await _dbSet.Where(n => n.IsActive).AsNoTracking().ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<Notification> AddAsync(Notification entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Notification> UpdateAsync(int id, Notification entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Notification not found.");
            }

            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();
            return existingEntity;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Notification not found.");
            }

            // Soft delete as requested by guidelines/instructions
            entity.IsActive = false;
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.AnyAsync(n => n.Id == id && n.IsActive);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userNic, string role)
        {
            return await _dbSet
                .Where(n => n.RecipientNic == userNic && n.RecipientRole == role && n.IsActive)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userNic, string role)
        {
            return await _dbSet
                .CountAsync(n => n.RecipientNic == userNic && n.RecipientRole == role && !n.IsRead && n.IsActive);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var entity = await _dbSet.FindAsync(notificationId);
            if (entity != null && !entity.IsRead)
            {
                entity.IsRead = true;
                entity.ReadDate = DateTime.UtcNow;
                _context.Entry(entity).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userNic, string role)
        {
            var unread = await _dbSet
                .Where(n => n.RecipientNic == userNic && n.RecipientRole == role && !n.IsRead && n.IsActive)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadDate = DateTime.UtcNow;
                _context.Entry(n).State = EntityState.Modified;
            }

            if (unread.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}