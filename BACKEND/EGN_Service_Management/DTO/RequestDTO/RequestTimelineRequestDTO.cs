using System.ComponentModel.DataAnnotations;

namespace 
EGN_Service_Management.DTO.RequestDTO
{
    public class RequestTimelineRequestDTO
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestId { get; set; } = string.Empty;

        public int Step { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public DateTime? Timestamp { get; set; }

        public bool Completed { get; set; }

        [MaxLength(20)]
        public string? OfficerNic { get; set; }

    }
}
