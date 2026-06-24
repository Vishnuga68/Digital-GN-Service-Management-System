using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IDocumentCategoryRepository
    {
        Task<IEnumerable<DocumentCategory>> GetAllAsync();
        Task<DocumentCategory?> GetByIdAsync(int id);
        Task<DocumentCategory> AddAsync(DocumentCategory entity);
        Task UpdateAsync(DocumentCategory entity);
        Task DeleteAsync(int id);
    }
}