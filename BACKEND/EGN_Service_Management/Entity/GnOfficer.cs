using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("gn_officers")]
public class GnOfficer
{
    [Key]
    [MaxLength(20)]
    public string Nic { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? GnDivisionId { get; set; }

    public Account? Account { get; set; }
    public GnDivision? GnDivision { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<TransferRequest> TransferRequests { get; set; } = new List<TransferRequest>();
}
}
