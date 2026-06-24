using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<ChatMessage> _dbSet;

        public ChatMessageRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<ChatMessage>();
        }

        public async Task<IEnumerable<ChatMessage>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<ChatMessage?> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<ChatMessage> AddAsync(ChatMessage entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<ChatMessage> UpdateAsync(string id, ChatMessage entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("ChatMessage not found.");
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
                throw new KeyNotFoundException("ChatMessage not found.");
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