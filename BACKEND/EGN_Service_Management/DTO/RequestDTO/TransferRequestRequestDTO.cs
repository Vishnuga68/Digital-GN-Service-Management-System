using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.RequestDTO
{
    public class TransferRequestRequestDTO
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string GnOfficerNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CurrentDivisionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RequestedDivisionId { get; set; } = string.Empty;

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        [MaxLength(255)]
        public string? DocumentName { get; set; }

        [MaxLength(255)]
        public string? DocumentUrl { get; set; }

        public DateOnly SubmittedDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        [MaxLength(20)]
        public string? ReviewedByNic { get; set; }

        public DateOnly? ReviewedDate { get; set; }

    }
}
