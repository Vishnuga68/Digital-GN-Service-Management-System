using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO
{
    public class CitizenRequestSubmissionDTO
    {
        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string GnDivisionId { get; set; } = string.Empty;

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string JsonData { get; set; } = string.Empty;

        public IFormFileCollection? Attachments { get; set; }
    }
}
