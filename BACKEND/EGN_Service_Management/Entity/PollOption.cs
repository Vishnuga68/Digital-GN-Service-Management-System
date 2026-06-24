using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("poll_options")]
public class PollOption
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AnnouncementId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OptionText { get; set; } = string.Empty;

    public Announcement? Announcement { get; set; }
    public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();
}
}
