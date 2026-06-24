using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.ResponseDTO
{
    public class NotificationResponseDTO
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string RecipientNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
