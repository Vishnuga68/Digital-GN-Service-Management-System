using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("gn_documents")]
    public class GNDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DocumentId { get; set; }

        [Required]
        [MaxLength(20)]
        public string GnOfficerNic { get; set; } = string.Empty; // Mapped to GnOfficer

        [Required]
        [MaxLength(50)]
        public string DivisionId { get; set; } = string.Empty; // Mapped to GnDivision

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft / Published

        public DateTime? PublishDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("GnOfficerNic")]
        public GnOfficer GnOfficer { get; set; }

        [ForeignKey("DivisionId")]
        public GnDivision GnDivision { get; set; }

        [ForeignKey("CategoryId")]
        public DocumentCategory Category { get; set; }
    }
}
