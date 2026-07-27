using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("accounts")]
public class Account
{
    [Key]
    [MaxLength(20)]
    public string Nic { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;



    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    public DateTime? LastLogin { get; set; }

    public DsOfficer? DsOfficer { get; set; }
    public GnOfficer? GnOfficer { get; set; }
    public EgnServiceUser? EgnServiceUser { get; set; }
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    // public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
}
