using System;

namespace EGN_Service_Management.DTO.ResponseDTO
{
    public class DocumentCategoryResponseDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
