using System;
using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class CreateCitizenDTO
    {
        [Required]
        [MaxLength(20)]
        public string Nic { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        public DateOnly? Dob { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(20)]
        public string? MaritalStatus { get; set; }

        [MaxLength(50)]
        public string? Religion { get; set; }

        [MaxLength(100)]
        public string? Occupation { get; set; }

        [MaxLength(50)]
        public string? FamilyCardNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
