using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("chat_messages")]
public class ChatMessage
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ConversationId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string SenderNic { get; set; } = string.Empty;

    [Required]
    public string MessageText { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "sent";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation? Conversation { get; set; }
    public Account? Sender { get; set; }
    public ICollection<ChatAttachment> Attachments { get; set; } = new List<ChatAttachment>();
}
}
