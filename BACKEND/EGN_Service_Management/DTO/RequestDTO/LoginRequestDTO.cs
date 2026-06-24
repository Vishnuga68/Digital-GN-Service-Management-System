using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class LoginRequestDTO
    {
        [Required]
        [MaxLength(20)]
        public string Nic { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? SelectedRole { get; set; }
    }
}
