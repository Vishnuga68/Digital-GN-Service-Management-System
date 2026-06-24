using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class CreateGnOfficerDTO
    {
        [Required]
        [MaxLength(20)]
        public string Nic { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? GnDivisionId { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public bool ForceReassign { get; set; } = false;
    }
}
