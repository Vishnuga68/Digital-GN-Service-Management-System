using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.RequestDTO
{
    public class ServiceRequestRequestDTO
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string GnDivisionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ServiceType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public int CurrentStep { get; set; } = 1;

        public string? RejectionReason { get; set; }

        [MaxLength(255)]
        public string? FinalDocumentUrl { get; set; }

        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    }
}
