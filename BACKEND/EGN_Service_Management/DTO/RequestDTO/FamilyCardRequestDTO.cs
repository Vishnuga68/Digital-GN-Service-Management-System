using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.RequestDTO
{
    public class FamilyCardRequestDTO
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

    }
}
