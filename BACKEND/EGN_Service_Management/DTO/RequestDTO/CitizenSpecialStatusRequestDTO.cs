using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.RequestDTO
{
    public class CitizenSpecialStatusRequestDTO
    {
        [Key]
        [MaxLength(20)]
        public string CitizenNic { get; set; } = string.Empty;

        public bool HasVoterId { get; set; }

        public bool IsNalanpuri { get; set; }

        public bool HasKidneyDisease { get; set; }

        public bool HasCancer { get; set; }

        public bool HasElderlyEligible { get; set; }

        public string? WelfareDetails { get; set; }

    }
}
