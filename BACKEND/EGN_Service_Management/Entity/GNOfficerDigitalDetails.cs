using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("gn_officer_digital_details")]
    public class GNOfficerDigitalDetails
    {
        [Key]
        [MaxLength(20)]
        public string GNOfficerNIC { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RubberStampPath { get; set; }

        [MaxLength(500)]
        public string? SignaturePath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        
        [MaxLength(100)]
        public string? CreatedBy { get; set; }
        
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation property back to GnOfficer
        [ForeignKey("GNOfficerNIC")]
        public virtual GnOfficer? GnOfficer { get; set; }
    }
}
