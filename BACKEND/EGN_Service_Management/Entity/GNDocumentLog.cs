using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("gn_document_logs")]
    public class GNDocumentLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; }

        public int DocumentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string PerformedByNic { get; set; } = string.Empty; // Mapped to GnOfficer

        [ForeignKey("PerformedByNic")]
        public GnOfficer PerformedBy { get; set; }
    }
}
