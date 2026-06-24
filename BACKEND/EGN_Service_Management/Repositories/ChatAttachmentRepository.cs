using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class ChatAttachmentRepository : IChatAttachmentRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<ChatAttachment> _dbSet;

        public ChatAttachmentRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<ChatAttachment>();
        }

        public async Task<IEnumerable<ChatAttachment>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<ChatAttachment?> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<ChatAttachment> AddAsync(ChatAttachment entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<ChatAttachment> UpdateAsync(string id, ChatAttachment entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("ChatAttachment not found.");
            }

            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();
            return existingEntity;
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("ChatAttachment not found.");
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _dbSet.FindAsync(id) != null;
        }
    }
}