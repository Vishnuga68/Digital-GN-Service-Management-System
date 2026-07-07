using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("gn_divisions")]
public class GnDivision
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DsDivisionId { get; set; } = string.Empty;

    public DsDivision? DsDivision { get; set; }
    public GnOfficer? GnOfficer { get; set; }
    public ICollection<CitizenMaster> Citizens { get; set; } = new List<CitizenMaster>();
    public ICollection<FamilyCard> FamilyCards { get; set; } = new List<FamilyCard>();
}
}
