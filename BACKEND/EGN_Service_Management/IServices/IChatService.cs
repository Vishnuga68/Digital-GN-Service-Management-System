using System.Collections.Generic;
using System.Threading.Tasks;
using EGN_Service_Management.DTO;

namespace EGN_Service_Management.IServices
{
    public interface IChatService
    {
        Task<ChatConversationDTO> GetOrCreateCommonConversationAsync(string citizenNic, string creatorNic);
        Task<ChatConversationDTO> GetOrCreateRequestConversationAsync(int requestId, string callerNic);
        Task<IEnumerable<ChatConversationDTO>> GetConversationsForUserAsync(string nic, string role);
        Task<IEnumerable<ChatMessageDTO>> GetMessagesAsync(int conversationId, string callerNic, int page, int pageSize);
        Task<ChatMessageDTO> SendMessageAsync(int conversationId, string senderNic, string senderRole, string text);
        Task<int> MarkMessagesAsReadAsync(int conversationId, string callerNic);
        Task<ChatUnreadCountDTO> GetUnreadCountsAsync(string nic, string role);
    }
}
