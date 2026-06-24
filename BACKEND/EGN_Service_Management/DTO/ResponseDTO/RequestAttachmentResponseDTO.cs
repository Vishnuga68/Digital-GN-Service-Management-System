using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.ResponseDTO
{
    public class RequestAttachmentResponseDTO
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RequestId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string FileSize { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    }
}
