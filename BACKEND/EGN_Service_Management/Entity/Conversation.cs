using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("conversations")]
public class Conversation
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CitizenNic { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string GnOfficerNic { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RequestId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public EgnServiceUser? Citizen { get; set; }
    public GnOfficer? GnOfficer { get; set; }
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
}
