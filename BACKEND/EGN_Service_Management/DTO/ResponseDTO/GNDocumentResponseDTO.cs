using System;

namespace EGN_Service_Management.DTO.ResponseDTO
{
    public class GNDocumentResponseDTO
    {
        public int DocumentId { get; set; }
        public string GnOfficerNic { get; set; } = string.Empty;
        public string DivisionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
