using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Citizen_Welfare_Status")]
    public class CitizenWelfareStatus
    {
        [Key]
        [MaxLength(20)]
        public string CitizenNic { get; set; }

        [Required]
        public bool IsAswesuma { get; set; }

        [Required]
        public bool IsDisabilityEligible { get; set; }

        [Required]
        public bool IsElderlyEligible { get; set; }

        [Required]
        public bool IsKidneyPatient { get; set; }

        [Required]
        public bool IsPregnantMotherEligible { get; set; }

        [Required]
        public bool IsPublicAssistanceEligible { get; set; }

        [Required]
        public bool IsHousingAssistanceEligible { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? LastVerifiedDate { get; set; }

        [MaxLength(20)]
        public string VerifiedByGnOfficer { get; set; }

        // Navigation properties
        [ForeignKey("CitizenNic")]
        public virtual CitizenMaster Citizen { get; set; }

        [ForeignKey("VerifiedByGnOfficer")]
        public virtual GnOfficer VerifyingOfficer { get; set; }
    }
}
