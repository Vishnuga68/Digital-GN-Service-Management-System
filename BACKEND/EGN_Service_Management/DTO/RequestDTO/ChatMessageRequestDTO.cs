using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.RequestDTO
{
    public class ChatMessageRequestDTO
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ConversationId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string SenderNic { get; set; } = string.Empty;

        [Required]
        public string MessageText { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "sent";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
