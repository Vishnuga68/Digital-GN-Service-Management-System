using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("gn_poll_options")]
    public class GnPollOption
    {
        [Key]
        public int OptionId { get; set; }

        [Required]
        public int AnnouncementId { get; set; }

        [Required]
        [MaxLength(255)]
        public string OptionText { get; set; } = string.Empty;

        [ForeignKey("AnnouncementId")]
        public GnAnnouncement? Announcement { get; set; }

        public ICollection<GnPollVote> PollVotes { get; set; } = new List<GnPollVote>();
    }
}
