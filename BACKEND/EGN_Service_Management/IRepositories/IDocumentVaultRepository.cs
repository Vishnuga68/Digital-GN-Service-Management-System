using EGN_Service_Management.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EGN_Service_Management.IRepositories
{
    public interface IDocumentVaultRepository
    {
        Task<IEnumerable<DocumentVaultDocument>> GetDocumentsAsync(string nic, string roleId);
        Task<DocumentVaultDocument> GetDocumentByIdAsync(int id, string nic, string roleId);
        Task<DocumentVaultDocument> AddDocumentAsync(DocumentVaultDocument document);
        Task UpdateDocumentAsync(DocumentVaultDocument document);
        Task RemoveDocumentAsync(DocumentVaultDocument document);
    }
}
