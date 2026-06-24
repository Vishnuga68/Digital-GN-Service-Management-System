using EGN_Service_Management.Entity;
using System.Threading.Tasks;

namespace EGN_Service_Management.IRepositories
{
    public interface IDocumentVaultCategoryRepository
    {
        Task<DocumentVaultCategory> GetCategoriesAsync(string nic, string roleId);
        Task UpdateCategoriesAsync(DocumentVaultCategory category);
        Task AddCategoriesAsync(DocumentVaultCategory category);
    }
}
