using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class EgnServiceUserRepository : IEgnServiceUserRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<EgnServiceUser> _dbSet;

        public EgnServiceUserRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<EgnServiceUser>();
        }

        public async Task<IEnumerable<EgnServiceUser>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<EgnServiceUser?> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<EgnServiceUser> AddAsync(EgnServiceUser entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<EgnServiceUser> UpdateAsync(string id, EgnServiceUser entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("EgnServiceUser not found.");
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
                throw new KeyNotFoundException("EgnServiceUser not found.");
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
