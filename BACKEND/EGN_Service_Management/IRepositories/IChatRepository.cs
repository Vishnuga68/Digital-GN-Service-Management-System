using System.Collections.Generic;
using System.Threading.Tasks;
using EGN_Service_Management.Entity.NewChat;

namespace EGN_Service_Management.IRepositories
{
    public interface IChatRepository
    {
        Task<ChatConversation?> GetConversationByIdAsync(int conversationId);
        Task<ChatConversation?> GetCommonConversationAsync(string citizenNic, string gnOfficerNic);
        Task<ChatConversation?> GetRequestConversationAsync(int requestId);
        Task<IEnumerable<ChatConversation>> GetConversationsForCitizenAsync(string citizenNic);
        Task<IEnumerable<ChatConversation>> GetConversationsForGnOfficerAsync(string gnOfficerNic);
        Task<IEnumerable<ChatMessage>> GetMessagesAsync(int conversationId, int page, int pageSize);
        Task<ChatMessage?> GetMessageByIdAsync(int messageId);
        Task<ChatConversation> AddConversationAsync(ChatConversation conversation);
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
        Task<ChatAttachment> AddAttachmentAsync(ChatAttachment attachment);
        Task<int> SaveChangesAsync();
    }
}
