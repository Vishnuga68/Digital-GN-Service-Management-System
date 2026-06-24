using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.ResponseDTO
{
    public class EgnServiceUserResponseDTO
    {
        [Key]
        [MaxLength(20)]
        public string Nic { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}
