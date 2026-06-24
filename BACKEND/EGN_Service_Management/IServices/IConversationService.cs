using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IConversationService
    {
        Task<IEnumerable<Conversation>> GetAllAsync();
        Task<Conversation> GetByIdAsync(string id);
        Task<Conversation> CreateAsync(Conversation entity);
        Task<Conversation> UpdateAsync(string id, Conversation entity);
        Task DeleteAsync(string id);
    }
}