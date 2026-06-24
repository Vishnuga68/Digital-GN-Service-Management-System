using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IChatAttachmentRepository
    {
        Task<IEnumerable<ChatAttachment>> GetAllAsync();
        Task<ChatAttachment?> GetByIdAsync(string id);
        Task<ChatAttachment> AddAsync(ChatAttachment entity);
        Task<ChatAttachment> UpdateAsync(string id, ChatAttachment entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}