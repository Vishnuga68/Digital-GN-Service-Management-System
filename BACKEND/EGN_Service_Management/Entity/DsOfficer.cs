using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("ds_officers")]
public class DsOfficer
{
    [Key]
    [MaxLength(20)]
    public string Nic { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? DsDivisionId { get; set; }

    public Account? Account { get; set; }
    public DsDivision? DsDivision { get; set; }
    public ICollection<TransferRequest> ReviewedTransferRequests { get; set; } = new List<TransferRequest>();
}
}
