using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class DocumentCategoryRequestDTO
    {
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
