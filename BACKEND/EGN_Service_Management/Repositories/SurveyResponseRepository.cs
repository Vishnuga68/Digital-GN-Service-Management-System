using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class SurveyResponseRepository : ISurveyResponseRepository
    {
        private readonly EgnDbContext _context;
        private readonly DbSet<SurveyResponse> _dbSet;

        public SurveyResponseRepository(EgnDbContext context)
        {
            _context = context;
            _dbSet = context.Set<SurveyResponse>();
        }

        public async Task<IEnumerable<SurveyResponse>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<SurveyResponse?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<SurveyResponse> AddAsync(SurveyResponse entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<SurveyResponse> UpdateAsync(int id, SurveyResponse entity)
        {
            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("SurveyResponse not found.");
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
                throw new KeyNotFoundException("SurveyResponse not found.");
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