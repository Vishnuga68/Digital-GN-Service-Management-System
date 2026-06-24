using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
[Table("announcements")]
public class Announcement
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string GnOfficerNic { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public bool Priority { get; set; }

    [MaxLength(100)]
    public string? ActionLabel { get; set; }

    [MaxLength(255)]
    public string? ActionLink { get; set; }

    [MaxLength(255)]
    public string? AttachmentName { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Published";

    [MaxLength(20)]
    public string? EstimatedTime { get; set; }

    [MaxLength(255)]
    public string? SurveyLink { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public GnOfficer? GnOfficer { get; set; }
    public ICollection<AnnouncementTargetCitizen> TargetCitizens { get; set; } = new List<AnnouncementTargetCitizen>();
    public ICollection<PollOption> PollOptions { get; set; } = new List<PollOption>();
    public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();
    public ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
}
