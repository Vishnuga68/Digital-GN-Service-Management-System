using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class ForgotPasswordRequestDTO
    {
        [Required]
        [MaxLength(20)]
        public string Nic { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
    }
}
