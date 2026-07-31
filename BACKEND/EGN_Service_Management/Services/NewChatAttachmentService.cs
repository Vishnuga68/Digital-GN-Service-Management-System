using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EGN_Service_Management.Database;
using EGN_Service_Management.DTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.Entity.NewChat;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Services
{
    public class NewChatAttachmentService : INewChatAttachmentService
    {
        private readonly IChatRepository _chatRepository;
        private readonly EgnDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NewChatAttachmentService(IChatRepository chatRepository, EgnDbContext context, IWebHostEnvironment env)
        {
            _chatRepository = chatRepository;
            _context = context;
            _env = env;
        }

        public async Task<ChatAttachmentDTO> UploadAttachmentAsync(int conversationId, int messageId, string uploadedByNic, IFormFile file, bool isSupportingDocument)
        {
            var conversation = await _chatRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
            {
                throw new KeyNotFoundException("Conversation not found.");
            }

            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                throw new KeyNotFoundException("Message not found.");
            }

            // Verify user belongs to conversation
            if (conversation.CitizenNic != uploadedByNic && conversation.GnOfficerNic != uploadedByNic)
            {
                throw new UnauthorizedAccessException("You are not authorized to upload files to this conversation.");
            }

            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Invalid file.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".txt" };
            if (!allowedExts.Contains(extension))
            {
                throw new ArgumentException("Unsupported file type.");
            }

            // Save file physically
            var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "chats");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var uniqueFileName = $"Chat_{conversationId}_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/chats/{uniqueFileName}";

            var attachment = new EGN_Service_Management.Entity.NewChat.ChatAttachment
            {
                MessageId = messageId,
                ConversationId = conversationId,
                RequestId = conversation.RequestId,
                OriginalFileName = file.FileName,
                StoredFileName = uniqueFileName,
                RelativeFilePath = fileUrl,
                FileExtension = extension.TrimStart('.'),
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedByNic = uploadedByNic,
                UploadedDate = GetSriLankanTime(),
                IsSupportingDocument = isSupportingDocument,
                IsAddedToRequest = false,
                IsActive = true
            };

            var savedAttachment = await _chatRepository.AddAttachmentAsync(attachment);

            // Update message type if it was just Text or empty
            if (message.MessageType == "Text")
            {
                message.MessageType = "TextWithAttachment";
            }
            else if (string.IsNullOrEmpty(message.MessageText))
            {
                message.MessageType = "Attachment";
            }

            await _chatRepository.SaveChangesAsync();

            return new ChatAttachmentDTO
            {
                AttachmentId = savedAttachment.AttachmentId,
                MessageId = savedAttachment.MessageId,
                OriginalFileName = savedAttachment.OriginalFileName,
                StoredFileName = savedAttachment.StoredFileName,
                RelativeFilePath = savedAttachment.RelativeFilePath,
                FileExtension = savedAttachment.FileExtension,
                ContentType = savedAttachment.ContentType,
                FileSize = savedAttachment.FileSize,
                IsSupportingDocument = savedAttachment.IsSupportingDocument,
                IsAddedToRequest = savedAttachment.IsAddedToRequest
            };
        }

        public async Task<bool> AddAttachmentToRequestAsync(int attachmentId, string gnOfficerNic)
        {
            var attachment = await _context.NewChatAttachments
                .Include(a => a.Conversation)
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

            if (attachment == null)
            {
                throw new KeyNotFoundException("Chat attachment not found.");
            }

            // Verify GN Officer is the one assigned to the conversation
            if (attachment.Conversation.GnOfficerNic != gnOfficerNic)
            {
                throw new UnauthorizedAccessException("You are not authorized to add this attachment to the request.");
            }

            if (!attachment.Conversation.RequestId.HasValue)
            {
                throw new InvalidOperationException("This conversation is not linked to any request.");
            }

            var requestId = attachment.Conversation.RequestId.Value;

            // Create UnifiedRequestAttachment
            var requestAttachment = new UnifiedRequestAttachment
            {
                RequestId = requestId,
                FileName = attachment.OriginalFileName,
                FileUrl = attachment.RelativeFilePath,
                FileType = attachment.ContentType,
                UploadedAt = GetSriLankanTime(),
                DocumentRole = "SupportingDocument",
                DisplayName = attachment.OriginalFileName,
                IsPrimary = false,
                UploadedBy = gnOfficerNic,
                FileSize = attachment.FileSize,
                MimeType = attachment.ContentType,
                IsActive = true
            };

            await _context.UnifiedRequestAttachments.AddAsync(requestAttachment);
            await _context.SaveChangesAsync();

            // Update ChatAttachment
            attachment.IsAddedToRequest = true;
            attachment.AddedToRequestDate = GetSriLankanTime();
            attachment.AddedToRequestByNic = gnOfficerNic;
            attachment.ExistingRequestAttachmentId = requestAttachment.AttachmentId;

            await _chatRepository.SaveChangesAsync();
            return true;
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
