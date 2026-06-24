using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class DocumentVaultRequestDTO
    {
        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; }

        [Required]
        [MaxLength(255)]
        public string DocumentName { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }
}
