using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EGN_Service_Management.Database;
using EGN_Service_Management.Entity.NewChat;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly EgnDbContext _context;

        public ChatRepository(EgnDbContext context)
        {
            _context = context;
        }

        public async Task<ChatConversation?> GetConversationByIdAsync(int conversationId)
        {
            return await _context.ChatConversations
                .Include(c => c.Citizen)
                .Include(c => c.GnOfficer)
                .Include(c => c.Request)
                    .ThenInclude(r => r.Category)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        public async Task<ChatConversation?> GetCommonConversationAsync(string citizenNic, string gnOfficerNic)
        {
            return await _context.ChatConversations
                .Include(c => c.Citizen)
                .Include(c => c.GnOfficer)
                .Include(c => c.Request)
                .FirstOrDefaultAsync(c => c.ChatType == "Common" && c.CitizenNic == citizenNic && c.GnOfficerNic == gnOfficerNic);
        }

        public async Task<ChatConversation?> GetRequestConversationAsync(int requestId)
        {
            return await _context.ChatConversations
                .Include(c => c.Citizen)
                .Include(c => c.GnOfficer)
                .Include(c => c.Request)
                    .ThenInclude(r => r.Category)
                .FirstOrDefaultAsync(c => c.ChatType == "RequestSupport" && c.RequestId == requestId);
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsForCitizenAsync(string citizenNic)
        {
            return await _context.ChatConversations
                .Include(c => c.Citizen)
                .Include(c => c.GnOfficer)
                .Include(c => c.Request)
                    .ThenInclude(r => r.Category)
                .Where(c => c.CitizenNic == citizenNic)
                .OrderByDescending(c => c.LastMessageDate ?? c.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsForGnOfficerAsync(string gnOfficerNic)
        {
            return await _context.ChatConversations
                .Include(c => c.Citizen)
                .Include(c => c.GnOfficer)
                .Include(c => c.Request)
                    .ThenInclude(r => r.Category)
                .Where(c => c.GnOfficerNic == gnOfficerNic)
                .OrderByDescending(c => c.LastMessageDate ?? c.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(int conversationId, int page, int pageSize)
        {
            var messages = await _context.NewChatMessages
                .Include(m => m.Attachments)
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderByDescending(m => m.SentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return messages.OrderBy(m => m.SentDate);
        }

        public async Task<ChatMessage?> GetMessageByIdAsync(int messageId)
        {
            return await _context.NewChatMessages
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public async Task<ChatConversation> AddConversationAsync(ChatConversation conversation)
        {
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            _context.NewChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<ChatAttachment> AddAttachmentAsync(ChatAttachment attachment)
        {
            _context.NewChatAttachments.Add(attachment);
            await _context.SaveChangesAsync();
            return attachment;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
