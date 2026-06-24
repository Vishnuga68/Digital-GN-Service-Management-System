using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class GNDocumentRepository : IGNDocumentRepository
    {
        private readonly EgnDbContext _context;

        public GNDocumentRepository(EgnDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GNDocument>> GetByDivisionIdAsync(string divisionId, string? status = null, int? categoryId = null)
        {
            var query = _context.GNDocuments
                .Include(d => d.Category)
                .Where(d => d.DivisionId == divisionId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(d => d.CategoryId == categoryId.Value);
            }

            return await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
        }

        public async Task<GNDocument?> GetByIdAsync(int id)
        {
            return await _context.GNDocuments
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.DocumentId == id);
        }

        public async Task<GNDocument> AddAsync(GNDocument entity)
        {
            await _context.GNDocuments.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(GNDocument entity)
        {
            _context.GNDocuments.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(GNDocument entity)
        {
            _context.GNDocuments.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddLogAsync(GNDocumentLog log)
        {
            await _context.GNDocumentLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}
