using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("announcement_target_citizens")]
public class AnnouncementTargetCitizen
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string AnnouncementId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CitizenNic { get; set; } = string.Empty;

    public Announcement? Announcement { get; set; }
    public CitizenMaster? Citizen { get; set; }
}
}
