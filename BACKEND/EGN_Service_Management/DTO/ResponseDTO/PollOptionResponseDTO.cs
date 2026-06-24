using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.ResponseDTO
{
    public class PollOptionResponseDTO
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string AnnouncementId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string OptionText { get; set; } = string.Empty;

    }
}
