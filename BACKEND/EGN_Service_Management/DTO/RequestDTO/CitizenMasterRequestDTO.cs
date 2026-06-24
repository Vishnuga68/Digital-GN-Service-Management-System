using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.RequestDTO
{
    public class CitizenMasterRequestDTO
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

    }
}
