using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("Citizen_Master")]
public class CitizenMaster
{
    [Key]
    [MaxLength(20)]
    public string Nic { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public DateOnly Dob { get; set; }

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string MaritalStatus { get; set; } = "Single";

    [MaxLength(50)]
    public string? Religion { get; set; }

    [MaxLength(100)]
    public string? Occupation { get; set; }

    [MaxLength(50)]
    public string? FamilyCardNumber { get; set; }

    [MaxLength(50)]
    public string? PastFamilyCardNumber { get; set; }

    [Required]
    [MaxLength(20)]
    public string LifeStatus { get; set; } = "Alive";

    [MaxLength(50)]
    public string? GnDivisionId { get; set; }

    public GnDivision? GnDivision { get; set; }
    public FamilyCard? FamilyCard { get; set; }
    public EgnServiceUser? EgnServiceUser { get; set; }
    public CitizenSpecialStatus? SpecialStatus { get; set; }
    public ICollection<FamilyCard> HeadedFamilyCards { get; set; } = new List<FamilyCard>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    public ICollection<AnnouncementTargetCitizen> AnnouncementTargets { get; set; } = new List<AnnouncementTargetCitizen>();
}
}
