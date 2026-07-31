using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity.NewChat
{
    [Table("Chat_Messages")]
    public class ChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MessageId { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string SenderNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SenderRole { get; set; } = string.Empty;

        public string? MessageText { get; set; }

        [Required]
        [MaxLength(50)]
        public string MessageType { get; set; } = string.Empty;

        [Required]
        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedDate { get; set; }

        // Navigation Properties
        [ForeignKey("ConversationId")]
        public virtual ChatConversation Conversation { get; set; }

        public virtual ICollection<ChatAttachment> Attachments { get; set; } = new List<ChatAttachment>();
    }
}
