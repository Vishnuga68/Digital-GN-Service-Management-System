using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity.NewChat
{
    [Table("Chat_Attachments")]
    public class ChatAttachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttachmentId { get; set; }

        [Required]
        public int MessageId { get; set; }

        [Required]
        public int ConversationId { get; set; }

        public int? RequestId { get; set; }

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string RelativeFilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string FileExtension { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [Required]
        [MaxLength(20)]
        public string UploadedByNic { get; set; } = string.Empty;

        [Required]
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsSupportingDocument { get; set; }

        [Required]
        public bool IsAddedToRequest { get; set; }

        public DateTime? AddedToRequestDate { get; set; }

        [MaxLength(20)]
        public string? AddedToRequestByNic { get; set; }

        public int? ExistingRequestAttachmentId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey("MessageId")]
        public virtual ChatMessage Message { get; set; }

        [ForeignKey("ConversationId")]
        public virtual ChatConversation Conversation { get; set; }

        [ForeignKey("RequestId")]
        public virtual UnifiedRequest Request { get; set; }

        [ForeignKey("ExistingRequestAttachmentId")]
        public virtual UnifiedRequestAttachment ExistingRequestAttachment { get; set; }
    }
}
