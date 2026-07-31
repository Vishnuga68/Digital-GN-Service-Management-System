using System;
using System.Collections.Generic;

namespace EGN_Service_Management.DTO
{
    public class ChatConversationDTO
    {
        public int ConversationId { get; set; }
        public string ChatType { get; set; } = string.Empty;
        public string CitizenNic { get; set; } = string.Empty;
        public string CitizenName { get; set; } = string.Empty;
        public string GnOfficerNic { get; set; } = string.Empty;
        public string GnOfficerName { get; set; } = string.Empty;
        public int? RequestId { get; set; }
        public string? RequestNumber { get; set; }
        public string? CategoryName { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
        public bool CanSendMessages { get; set; }
    }

    public class ChatMessageDTO
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public string SenderNic { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public string? MessageText { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }
        public bool IsOwnMessage { get; set; }
        public List<ChatAttachmentDTO> Attachments { get; set; } = new List<ChatAttachmentDTO>();
    }

    public class ChatAttachmentDTO
    {
        public int AttachmentId { get; set; }
        public int MessageId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string RelativeFilePath { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool IsSupportingDocument { get; set; }
        public bool IsAddedToRequest { get; set; }
    }

    public class SendMessageDTO
    {
        public string? MessageText { get; set; }
    }

    public class ChatUnreadCountDTO
    {
        public int TotalUnreadCount { get; set; }
        public List<ConversationUnreadCountDTO> Conversations { get; set; } = new List<ConversationUnreadCountDTO>();
    }

    public class ConversationUnreadCountDTO
    {
        public int ConversationId { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatAttachmentUploadDTO
    {
        public Microsoft.AspNetCore.Http.IFormFile File { get; set; } = null!;
    }
}
