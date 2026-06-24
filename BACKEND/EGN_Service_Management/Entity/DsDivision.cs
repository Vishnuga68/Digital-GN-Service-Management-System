using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
[Table("ds_divisions")]
public class DsDivision
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? DistrictId { get; set; }

    public int? ProvinceId { get; set; }

    [ForeignKey("DistrictId")]
    public District District { get; set; }

    [ForeignKey("ProvinceId")]
    public Province Province { get; set; }

    public ICollection<GnDivision> GnDivisions { get; set; } = new List<GnDivision>();
    public ICollection<DsOfficer> DsOfficers { get; set; } = new List<DsOfficer>();
}
}
