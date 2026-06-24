using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IRequestAttachmentService
    {
        Task<IEnumerable<RequestAttachment>> GetAllAsync();
        Task<RequestAttachment> GetByIdAsync(string id);
        Task<RequestAttachment> CreateAsync(RequestAttachment entity);
        Task<RequestAttachment> UpdateAsync(string id, RequestAttachment entity);
        Task DeleteAsync(string id);
    }
}