using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class CitizenMasterRepository : ICitizenMasterRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<CitizenMaster> _dbSet;

        public CitizenMasterRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<CitizenMaster>();
        }

        public async Task<IEnumerable<CitizenMaster>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<CitizenMaster?> GetByIdAsync(string id)
        {
            return await _dbSet.Include(x => x.FamilyCard).FirstOrDefaultAsync(x => x.Nic == id);
        }

        public async Task<CitizenMaster> AddAsync(CitizenMaster entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<CitizenMaster> UpdateAsync(string id, CitizenMaster entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("CitizenMaster not found.");
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
                throw new KeyNotFoundException("CitizenMaster not found.");
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