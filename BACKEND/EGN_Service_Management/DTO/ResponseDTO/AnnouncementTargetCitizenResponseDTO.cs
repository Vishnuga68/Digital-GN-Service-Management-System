using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.ResponseDTO
{
    public class AnnouncementTargetCitizenResponseDTO
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string AnnouncementId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; } = string.Empty;

    }
}
