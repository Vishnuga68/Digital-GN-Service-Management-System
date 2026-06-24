using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IConversationRepository
    {
        Task<IEnumerable<Conversation>> GetAllAsync();
        Task<Conversation?> GetByIdAsync(string id);
        Task<Conversation> AddAsync(Conversation entity);
        Task<Conversation> UpdateAsync(string id, Conversation entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}