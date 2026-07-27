using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("family_cards")]
public class FamilyCard
{
    [Key]
    [MaxLength(50)]
    public string FamilyCardNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string HeadOfHouseholdNic { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string GnDivisionId { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    public DateOnly RegisteredDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    [MaxLength(20)]
    public string? HouseNumber { get; set; }

    public CitizenMaster? HeadOfHousehold { get; set; }
    public GnDivision? GnDivision { get; set; }
    public ICollection<CitizenMaster> Citizens { get; set; } = new List<CitizenMaster>();
}
}
