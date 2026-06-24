using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IRequestAttachmentRepository
    {
        Task<IEnumerable<RequestAttachment>> GetAllAsync();
        Task<RequestAttachment?> GetByIdAsync(string id);
        Task<RequestAttachment> AddAsync(RequestAttachment entity);
        Task<RequestAttachment> UpdateAsync(string id, RequestAttachment entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}