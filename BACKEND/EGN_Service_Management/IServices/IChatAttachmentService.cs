using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IChatAttachmentService
    {
        Task<IEnumerable<ChatAttachment>> GetAllAsync();
        Task<ChatAttachment> GetByIdAsync(string id);
        Task<ChatAttachment> CreateAsync(ChatAttachment entity);
        Task<ChatAttachment> UpdateAsync(string id, ChatAttachment entity);
        Task DeleteAsync(string id);
    }
}