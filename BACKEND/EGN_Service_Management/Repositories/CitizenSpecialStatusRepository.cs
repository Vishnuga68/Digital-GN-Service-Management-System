using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class CitizenSpecialStatusRepository : ICitizenSpecialStatusRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<CitizenSpecialStatus> _dbSet;

        public CitizenSpecialStatusRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<CitizenSpecialStatus>();
        }

        public async Task<IEnumerable<CitizenSpecialStatus>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<CitizenSpecialStatus?> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<CitizenSpecialStatus> AddAsync(CitizenSpecialStatus entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<CitizenSpecialStatus> UpdateAsync(string id, CitizenSpecialStatus entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("CitizenSpecialStatus not found.");
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
                throw new KeyNotFoundException("CitizenSpecialStatus not found.");
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