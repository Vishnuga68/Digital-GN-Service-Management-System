using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("poll_votes")]
public class PollVote
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string AnnouncementId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CitizenNic { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string PollOptionId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Announcement? Announcement { get; set; }
    public EgnServiceUser? Citizen { get; set; }
    public PollOption? PollOption { get; set; }
}
}
