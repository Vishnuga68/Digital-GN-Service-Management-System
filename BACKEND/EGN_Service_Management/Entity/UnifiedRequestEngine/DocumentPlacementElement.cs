using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Document_Placement_Elements")]
    public class DocumentPlacementElement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ElementId { get; set; }

        [Required]
        public int VerificationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ElementType { get; set; } = string.Empty; // SIGNATURE, RUBBER_STAMP, QR_CODE, VERIFIED_DATE, GN_OFFICER_NAME, VERIFICATION_TEXT

        [Required]
        public int PageNumber { get; set; }

        [Required]
        public double X { get; set; }

        [Required]
        public double Y { get; set; }

        [Required]
        public double Width { get; set; }

        [Required]
        public double Height { get; set; }

        public double Rotation { get; set; }

        public int LayerOrder { get; set; }

        [MaxLength(2000)]
        public string? TextValue { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("VerificationId")]
        public virtual DocumentVerification? Verification { get; set; }
    }
}
