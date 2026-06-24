using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class GnDivisionRepository : IGnDivisionRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<GnDivision> _dbSet;

        public GnDivisionRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<GnDivision>();
        }

        public async Task<IEnumerable<GnDivision>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<GnDivision?> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<GnDivision> AddAsync(GnDivision entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<GnDivision> UpdateAsync(string id, GnDivision entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("GnDivision not found.");
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
                throw new KeyNotFoundException("GnDivision not found.");
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