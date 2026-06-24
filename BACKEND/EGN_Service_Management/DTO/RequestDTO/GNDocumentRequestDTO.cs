using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class GNDocumentRequestDTO
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool PublishNow { get; set; } // Set to true to publish immediately
    }
}
