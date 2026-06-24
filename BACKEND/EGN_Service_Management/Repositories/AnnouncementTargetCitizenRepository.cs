using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class AnnouncementTargetCitizenRepository : IAnnouncementTargetCitizenRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<AnnouncementTargetCitizen> _dbSet;

        public AnnouncementTargetCitizenRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<AnnouncementTargetCitizen>();
        }

        public async Task<IEnumerable<AnnouncementTargetCitizen>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<AnnouncementTargetCitizen?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<AnnouncementTargetCitizen> AddAsync(AnnouncementTargetCitizen entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<AnnouncementTargetCitizen> UpdateAsync(int id, AnnouncementTargetCitizen entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("AnnouncementTargetCitizen not found.");
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
                throw new KeyNotFoundException("AnnouncementTargetCitizen not found.");
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.FindAsync(id) != null;
        }
    }
}