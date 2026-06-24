using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EGN_Service_Management.Repositories
{
    public class DocumentVaultRepository : IDocumentVaultRepository
    {
        private readonly EgnDbContext _context;

        public DocumentVaultRepository(EgnDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DocumentVaultDocument>> GetDocumentsAsync(string nic, string roleId)
        {
            return await _context.DocumentVaultDocuments
                .Where(d => d.NIC == nic && d.RoleId == roleId && d.IsActive)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<DocumentVaultDocument> GetDocumentByIdAsync(int id, string nic, string roleId)
        {
            return await _context.DocumentVaultDocuments
                .FirstOrDefaultAsync(d => d.VaultDocumentId == id && d.NIC == nic && d.RoleId == roleId && d.IsActive);
        }

        public async Task<DocumentVaultDocument> AddDocumentAsync(DocumentVaultDocument document)
        {
            _context.DocumentVaultDocuments.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task UpdateDocumentAsync(DocumentVaultDocument document)
        {
            _context.DocumentVaultDocuments.Update(document);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveDocumentAsync(DocumentVaultDocument document)
        {
            _context.DocumentVaultDocuments.Remove(document);
            await _context.SaveChangesAsync();
        }
    }
}
