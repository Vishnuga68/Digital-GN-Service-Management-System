using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EGN_Service_Management.Repositories
{
    public class DocumentVaultCategoryRepository : IDocumentVaultCategoryRepository
    {
        private readonly EgnDbContext _context;

        public DocumentVaultCategoryRepository(EgnDbContext context)
        {
            _context = context;
        }

        public async Task<DocumentVaultCategory> GetCategoriesAsync(string nic, string roleId)
        {
            return await _context.DocumentVaultCategories
                .FirstOrDefaultAsync(c => c.NIC == nic && c.RoleId == roleId);
        }

        public async Task AddCategoriesAsync(DocumentVaultCategory category)
        {
            await _context.DocumentVaultCategories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoriesAsync(DocumentVaultCategory category)
        {
            _context.DocumentVaultCategories.Update(category);
            await _context.SaveChangesAsync();
        }
    }
}
