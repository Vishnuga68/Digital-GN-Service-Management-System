using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IChatMessageService
    {
        Task<IEnumerable<ChatMessage>> GetAllAsync();
        Task<ChatMessage> GetByIdAsync(string id);
        Task<ChatMessage> CreateAsync(ChatMessage entity);
        Task<ChatMessage> UpdateAsync(string id, ChatMessage entity);
        Task DeleteAsync(string id);
    }
}