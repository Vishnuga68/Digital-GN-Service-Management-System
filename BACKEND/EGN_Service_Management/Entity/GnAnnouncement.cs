using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("gn_announcements")]
    public class GnAnnouncement
    {
        [Key]
        public int AnnouncementId { get; set; }

        [Required]
        [MaxLength(20)]
        public string GnOfficerNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string GnDivisionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string AnnouncementType { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? DocumentId { get; set; }

        public string? ExternalSurveyUrl { get; set; }

        public string? PollQuestion { get; set; }

        public DateTime? PublishDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string? CreatedBy { get; set; }

        [MaxLength(20)]
        public string? ModifiedBy { get; set; }

        // Navigation properties
        [ForeignKey("GnOfficerNic")]
        public GnOfficer? GnOfficer { get; set; }

        [ForeignKey("GnDivisionId")]
        public GnDivision? GnDivision { get; set; }

        [ForeignKey("DocumentId")]
        public GNDocument? GNDocument { get; set; }

        public ICollection<GnPollOption> PollOptions { get; set; } = new List<GnPollOption>();
        public ICollection<GnPollVote> PollVotes { get; set; } = new List<GnPollVote>();
    }
}
