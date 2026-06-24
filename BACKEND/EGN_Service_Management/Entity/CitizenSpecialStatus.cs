using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("citizen_special_statuses")]
public class CitizenSpecialStatus
{
    [Key]
    [MaxLength(20)]
    public string CitizenNic { get; set; } = string.Empty;

    public bool HasVoterId { get; set; }
    public bool IsNalanpuri { get; set; }
    public bool HasKidneyDisease { get; set; }
    public bool HasCancer { get; set; }
    public bool HasElderlyEligible { get; set; }
    [NotMapped]
    public string? WelfareDetails { get; set; }

    public CitizenMaster? CitizenMaster { get; set; }
}
}
