using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
[Table("service_requests")]
public class ServiceRequest
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CitizenNic { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string GnDivisionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ServiceType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    public int CurrentStep { get; set; } = 1;
    public string? RejectionReason { get; set; }

    [MaxLength(255)]
    public string? FinalDocumentUrl { get; set; }

    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public CitizenMaster? Citizen { get; set; }
    public GnDivision? GnDivision { get; set; }
    public ICollection<RequestTimeline> Timelines { get; set; } = new List<RequestTimeline>();
    public ICollection<RequestAttachment> Attachments { get; set; } = new List<RequestAttachment>();
    public ICollection<RequestRemark> Remarks { get; set; } = new List<RequestRemark>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
}
