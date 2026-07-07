using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("gn_poll_votes")]
    public class GnPollVote
    {
        [Key]
        public int VoteId { get; set; }

        [Required]
        public int AnnouncementId { get; set; }

        [Required]
        public int OptionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; } = string.Empty;

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("AnnouncementId")]
        public GnAnnouncement? Announcement { get; set; }

        [ForeignKey("OptionId")]
        public GnPollOption? Option { get; set; }

        [ForeignKey("CitizenNic")]
        public CitizenMaster? Citizen { get; set; }
    }
}
