using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EGN_Service_Management.Database;
using EGN_Service_Management.DTO;
using EGN_Service_Management.Entity.NewChat;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly EgnDbContext _context;

        public ChatService(IChatRepository chatRepository, EgnDbContext context)
        {
            _chatRepository = chatRepository;
            _context = context;
        }

        public async Task<ChatConversationDTO> GetOrCreateCommonConversationAsync(string citizenNic, string creatorNic)
        {
            // 1. Find Citizen and GN Division
            var citizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == citizenNic);
            if (citizen == null)
            {
                throw new KeyNotFoundException("Citizen not found.");
            }

            if (string.IsNullOrEmpty(citizen.GnDivisionId))
            {
                var familyCard = await _context.FamilyCards.FirstOrDefaultAsync(fc => fc.FamilyCardNumber == citizen.FamilyCardNumber && fc.Status == "Active");
                if (familyCard != null)
                {
                    citizen.GnDivisionId = familyCard.GnDivisionId;
                }
            }

            if (string.IsNullOrEmpty(citizen.GnDivisionId))
            {
                throw new InvalidOperationException("Citizen has no assigned GN Division.");
            }

            // 2. Find Assigned GN Officer
            var gnOfficerNic = await _context.GnOfficers
                .Where(g => g.GnDivisionId == citizen.GnDivisionId)
                .Select(g => g.Nic)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(gnOfficerNic))
            {
                throw new InvalidOperationException("No active GN Officer is assigned to your division.");
            }

            // 3. Look up or Create conversation
            ChatConversation conversation;
            try
            {
                var existing = await _chatRepository.GetCommonConversationAsync(citizenNic, gnOfficerNic);
                if (existing != null)
                {
                    conversation = existing;
                }
                else
                {
                    var newConv = new ChatConversation
                    {
                        ChatType = "Common",
                        CitizenNic = citizenNic,
                        GnOfficerNic = gnOfficerNic,
                        CreatedByNic = creatorNic,
                        CreatedDate = GetSriLankanTime(),
                        IsActive = true,
                        IsClosed = false
                    };
                    conversation = await _chatRepository.AddConversationAsync(newConv);
                }
            }
            catch (DbUpdateException)
            {
                // Handles simultaneous creation constraint errors
                var reloaded = await _chatRepository.GetCommonConversationAsync(citizenNic, gnOfficerNic);
                if (reloaded == null)
                {
                    throw new InvalidOperationException("Failed to find or create conversation.");
                }
                conversation = reloaded;
            }

            // Ensure navigation properties are fully loaded
            var fullyLoadedConv = await _chatRepository.GetConversationByIdAsync(conversation.ConversationId);
            return await MapToDTOAsync(fullyLoadedConv ?? conversation, creatorNic);
        }

        public async Task<ChatConversationDTO> GetOrCreateRequestConversationAsync(int requestId, string callerNic)
        {
            // 1. Validate request exists
            var request = await _context.UnifiedRequests
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (request == null)
            {
                throw new KeyNotFoundException("Request not found.");
            }

            // 2. Validate caller belongs to the request
            if (request.CitizenNic != callerNic && request.AssignedGnOfficerNic != callerNic)
            {
                throw new UnauthorizedAccessException("You do not belong to this service request.");
            }

            // 3. Find GN Officer assigned to request
            var gnOfficerNic = request.AssignedGnOfficerNic;
            if (string.IsNullOrEmpty(gnOfficerNic))
            {
                // Fallback to active GN Officer for division if not explicitly assigned
                gnOfficerNic = await _context.GnOfficers
                    .Where(g => g.GnDivisionId == request.GnDivision)
                    .Select(g => g.Nic)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrEmpty(gnOfficerNic))
            {
                throw new InvalidOperationException("No GN Officer is assigned to this request division.");
            }

            // 4. Look up or Create RequestSupport conversation
            ChatConversation conversation;
            try
            {
                var existing = await _chatRepository.GetRequestConversationAsync(requestId);
                if (existing != null)
                {
                    conversation = existing;
                }
                else
                {
                    var newConv = new ChatConversation
                    {
                        ChatType = "RequestSupport",
                        CitizenNic = request.CitizenNic,
                        GnOfficerNic = gnOfficerNic,
                        RequestId = requestId,
                        CreatedByNic = callerNic,
                        CreatedDate = GetSriLankanTime(),
                        IsActive = true,
                        IsClosed = false
                    };
                    conversation = await _chatRepository.AddConversationAsync(newConv);
                }
            }
            catch (DbUpdateException)
            {
                var reloaded = await _chatRepository.GetRequestConversationAsync(requestId);
                if (reloaded == null)
                {
                    throw new InvalidOperationException("Failed to find or create request conversation.");
                }
                conversation = reloaded;
            }

            var fullyLoadedConv = await _chatRepository.GetConversationByIdAsync(conversation.ConversationId);
            return await MapToDTOAsync(fullyLoadedConv ?? conversation, callerNic);
        }

        public async Task<IEnumerable<ChatConversationDTO>> GetConversationsForUserAsync(string nic, string role)
        {
            IEnumerable<ChatConversation> list;
            if (role.Equals("Citizen", StringComparison.OrdinalIgnoreCase))
            {
                list = await _chatRepository.GetConversationsForCitizenAsync(nic);
            }
            else if (role.Equals("GnOfficer", StringComparison.OrdinalIgnoreCase))
            {
                list = await _chatRepository.GetConversationsForGnOfficerAsync(nic);
            }
            else
            {
                return Enumerable.Empty<ChatConversationDTO>();
            }

            var dtoList = new List<ChatConversationDTO>();
            foreach (var conv in list)
            {
                dtoList.Add(await MapToDTOAsync(conv, nic));
            }
            return dtoList;
        }

        public async Task<IEnumerable<ChatMessageDTO>> GetMessagesAsync(int conversationId, string callerNic, int page, int pageSize)
        {
            var conv = await _chatRepository.GetConversationByIdAsync(conversationId);
            if (conv == null)
            {
                throw new KeyNotFoundException("Conversation not found.");
            }

            // Check caller access
            if (conv.CitizenNic != callerNic && conv.GnOfficerNic != callerNic)
            {
                throw new UnauthorizedAccessException("You are not authorized to view messages in this conversation.");
            }

            var messages = await _chatRepository.GetMessagesAsync(conversationId, page, pageSize);
            var dtoList = new List<ChatMessageDTO>();
            foreach (var msg in messages)
            {
                dtoList.Add(await MapToMessageDTOAsync(msg, callerNic));
            }
            return dtoList;
        }

        public async Task<ChatMessageDTO> SendMessageAsync(int conversationId, string senderNic, string senderRole, string text)
        {
            var conv = await _chatRepository.GetConversationByIdAsync(conversationId);
            if (conv == null)
            {
                throw new KeyNotFoundException("Conversation not found.");
            }

            // 1. Verify user belongs to conversation
            if (conv.CitizenNic != senderNic && conv.GnOfficerNic != senderNic)
            {
                throw new UnauthorizedAccessException("You are not authorized to send messages in this conversation.");
            }

            // 2. Check that conversation is active and request is not in a final state
            if (!conv.IsActive || conv.IsClosed)
            {
                throw new InvalidOperationException("This conversation has been closed or inactivated.");
            }

            if (conv.ChatType == "RequestSupport" && conv.RequestId.HasValue)
            {
                var request = await _context.UnifiedRequests.FirstOrDefaultAsync(r => r.RequestId == conv.RequestId.Value);
                if (request != null)
                {
                    var isFinal = request.CurrentStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase) ||
                                  request.CurrentStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
                                  request.CurrentStatus.Equals("Verified", StringComparison.OrdinalIgnoreCase) ||
                                  request.CurrentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                                  request.CurrentStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                    if (isFinal)
                    {
                        throw new InvalidOperationException("Cannot send messages because the request is in a final state.");
                    }
                }
            }

            // 3. Trim and validate message text
            var trimmedText = text?.Trim();
            if (string.IsNullOrEmpty(trimmedText))
            {
                throw new ArgumentException("Message content cannot be empty.");
            }

            // 4. Save message
            var message = new ChatMessage
            {
                ConversationId = conversationId,
                SenderNic = senderNic,
                SenderRole = senderRole,
                MessageText = trimmedText,
                MessageType = "Text",
                SentDate = GetSriLankanTime(),
                IsRead = false,
                IsDeleted = false
            };

            var savedMessage = await _chatRepository.AddMessageAsync(message);

            // 5. Update Conversation LastMessageDate
            conv.LastMessageDate = savedMessage.SentDate;
            await _chatRepository.SaveChangesAsync();

            return await MapToMessageDTOAsync(savedMessage, senderNic);
        }

        public async Task<int> MarkMessagesAsReadAsync(int conversationId, string callerNic)
        {
            var conv = await _chatRepository.GetConversationByIdAsync(conversationId);
            if (conv == null)
            {
                throw new KeyNotFoundException("Conversation not found.");
            }

            if (conv.CitizenNic != callerNic && conv.GnOfficerNic != callerNic)
            {
                throw new UnauthorizedAccessException("You are not authorized to access this conversation.");
            }

            // Mark other user's messages as read
            var unreadMessages = await _context.NewChatMessages
                .Where(m => m.ConversationId == conversationId && m.SenderNic != callerNic && !m.IsRead && !m.IsDeleted)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    msg.ReadDate = GetSriLankanTime();
                }
                await _chatRepository.SaveChangesAsync();
            }

            // Return updated unread count for this conversation
            return await _context.NewChatMessages
                .CountAsync(m => m.ConversationId == conversationId && m.SenderNic != callerNic && !m.IsRead && !m.IsDeleted);
        }

        public async Task<ChatUnreadCountDTO> GetUnreadCountsAsync(string nic, string role)
        {
            IEnumerable<ChatConversation> list;
            if (role.Equals("Citizen", StringComparison.OrdinalIgnoreCase))
            {
                list = await _chatRepository.GetConversationsForCitizenAsync(nic);
            }
            else if (role.Equals("GnOfficer", StringComparison.OrdinalIgnoreCase))
            {
                list = await _chatRepository.GetConversationsForGnOfficerAsync(nic);
            }
            else
            {
                return new ChatUnreadCountDTO { TotalUnreadCount = 0 };
            }

            var conversationsWithUnread = new List<ConversationUnreadCountDTO>();
            int total = 0;

            foreach (var conv in list)
            {
                var count = await _context.NewChatMessages
                    .CountAsync(m => m.ConversationId == conv.ConversationId && m.SenderNic != nic && !m.IsRead && !m.IsDeleted);
                
                conversationsWithUnread.Add(new ConversationUnreadCountDTO
                {
                    ConversationId = conv.ConversationId,
                    UnreadCount = count
                });
                total += count;
            }

            return new ChatUnreadCountDTO
            {
                TotalUnreadCount = total,
                Conversations = conversationsWithUnread
            };
        }

        // Helpers

        private async Task<ChatConversationDTO> MapToDTOAsync(ChatConversation conv, string callerNic)
        {
            var citizenName = conv.Citizen?.FullName ?? string.Empty;
            if (string.IsNullOrEmpty(citizenName) && !string.IsNullOrEmpty(conv.CitizenNic))
            {
                var cit = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == conv.CitizenNic);
                citizenName = cit?.FullName ?? string.Empty;
            }

            var gnOfficerName = conv.GnOfficer?.FullName ?? string.Empty;
            if (string.IsNullOrEmpty(gnOfficerName) && !string.IsNullOrEmpty(conv.GnOfficerNic))
            {
                var off = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == conv.GnOfficerNic);
                gnOfficerName = off?.FullName ?? string.Empty;
            }

            var lastMessage = await _context.NewChatMessages
                .Where(m => m.ConversationId == conv.ConversationId && !m.IsDeleted)
                .OrderByDescending(m => m.SentDate)
                .FirstOrDefaultAsync();

            var unreadCount = await _context.NewChatMessages
                .CountAsync(m => m.ConversationId == conv.ConversationId && m.SenderNic != callerNic && !m.IsRead && !m.IsDeleted);

            bool canSendMessages = true;
            if (conv.ChatType == "RequestSupport" && conv.RequestId.HasValue)
            {
                var request = conv.Request ?? await _context.UnifiedRequests.FirstOrDefaultAsync(r => r.RequestId == conv.RequestId.Value);
                if (request != null)
                {
                    var isFinal = request.CurrentStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase) ||
                                  request.CurrentStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
                                  request.CurrentStatus.Equals("Verified", StringComparison.OrdinalIgnoreCase) ||
                                  request.CurrentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                                  request.CurrentStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                    canSendMessages = !isFinal;
                }
            }
            else
            {
                canSendMessages = conv.IsActive && !conv.IsClosed;
            }

            return new ChatConversationDTO
            {
                ConversationId = conv.ConversationId,
                ChatType = conv.ChatType,
                CitizenNic = conv.CitizenNic,
                CitizenName = citizenName,
                GnOfficerNic = conv.GnOfficerNic,
                GnOfficerName = gnOfficerName,
                RequestId = conv.RequestId,
                RequestNumber = conv.Request?.RequestNumber,
                CategoryName = conv.Request?.Category?.CategoryName,
                LastMessage = lastMessage?.MessageText ?? (lastMessage != null && lastMessage.MessageType != "Text" ? "[Attachment]" : string.Empty),
                LastMessageDate = lastMessage?.SentDate ?? conv.CreatedDate,
                UnreadCount = unreadCount,
                CanSendMessages = canSendMessages
            };
        }

        private async Task<ChatMessageDTO> MapToMessageDTOAsync(ChatMessage msg, string callerNic)
        {
            var senderName = await GetSenderNameAsync(msg.SenderNic, msg.SenderRole);
            var attachments = msg.Attachments != null
                ? msg.Attachments.Select(a => new ChatAttachmentDTO
                {
                    AttachmentId = a.AttachmentId,
                    MessageId = a.MessageId,
                    OriginalFileName = a.OriginalFileName,
                    StoredFileName = a.StoredFileName,
                    RelativeFilePath = a.RelativeFilePath,
                    FileExtension = a.FileExtension,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    IsSupportingDocument = a.IsSupportingDocument,
                    IsAddedToRequest = a.IsAddedToRequest
                }).ToList()
                : new List<ChatAttachmentDTO>();

            return new ChatMessageDTO
            {
                MessageId = msg.MessageId,
                ConversationId = msg.ConversationId,
                SenderNic = msg.SenderNic,
                SenderName = senderName,
                SenderRole = msg.SenderRole,
                MessageText = msg.MessageText,
                MessageType = msg.MessageType,
                SentDate = msg.SentDate,
                IsRead = msg.IsRead,
                IsOwnMessage = msg.SenderNic == callerNic,
                Attachments = attachments
            };
        }

        private async Task<string> GetSenderNameAsync(string senderNic, string senderRole)
        {
            if (senderRole.Equals("Citizen", StringComparison.OrdinalIgnoreCase))
            {
                var citizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == senderNic);
                return citizen?.FullName ?? "Citizen";
            }
            else if (senderRole.Equals("GnOfficer", StringComparison.OrdinalIgnoreCase))
            {
                var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == senderNic);
                return officer?.FullName ?? "GN Officer";
            }
            return "System";
        }

        private DateTime GetSriLankanTime()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"));
            }
            catch
            {
                return DateTime.UtcNow.AddHours(5.5);
            }
        }
    }
}
