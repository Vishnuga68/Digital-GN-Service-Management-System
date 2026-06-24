using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class DocumentCategoryRepository : IDocumentCategoryRepository
    {
        private readonly EgnDbContext _context;

        public DocumentCategoryRepository(EgnDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DocumentCategory>> GetAllAsync()
        {
            return await _context.DocumentCategories.ToListAsync();
        }

        public async Task<DocumentCategory?> GetByIdAsync(int id)
        {
            return await _context.DocumentCategories.FindAsync(id);
        }

        public async Task<DocumentCategory> AddAsync(DocumentCategory entity)
        {
            await _context.DocumentCategories.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(DocumentCategory entity)
        {
            _context.DocumentCategories.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.DocumentCategories.FindAsync(id);
            if (entity != null)
            {
                _context.DocumentCategories.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}