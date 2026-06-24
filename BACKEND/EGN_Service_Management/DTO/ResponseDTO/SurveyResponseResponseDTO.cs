using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.ResponseDTO
{
    public class SurveyResponseResponseDTO
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string AnnouncementId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; } = string.Empty;

        [Required]
        public string ResponseText { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    }
}
