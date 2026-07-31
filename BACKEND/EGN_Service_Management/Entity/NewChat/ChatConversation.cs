using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity.NewChat
{
    [Table("Chat_Conversations")]
    public class ChatConversation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ConversationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChatType { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string GnOfficerNic { get; set; } = string.Empty;

        public int? RequestId { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastMessageDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string CreatedByNic { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsClosed { get; set; } = false;

        public DateTime? ClosedDate { get; set; }

        // Navigation Properties
        [ForeignKey("CitizenNic")]
        public virtual CitizenMaster Citizen { get; set; }

        [ForeignKey("GnOfficerNic")]
        public virtual GnOfficer GnOfficer { get; set; }

        [ForeignKey("RequestId")]
        public virtual UnifiedRequest Request { get; set; }

        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<ChatAttachment> ChatAttachments { get; set; } = new List<ChatAttachment>();
    }
}
