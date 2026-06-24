using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IChatMessageRepository
    {
        Task<IEnumerable<ChatMessage>> GetAllAsync();
        Task<ChatMessage?> GetByIdAsync(string id);
        Task<ChatMessage> AddAsync(ChatMessage entity);
        Task<ChatMessage> UpdateAsync(string id, ChatMessage entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}