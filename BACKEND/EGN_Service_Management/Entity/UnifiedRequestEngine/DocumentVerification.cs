using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Document_Verifications")]
    public class DocumentVerification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VerificationId { get; set; }

        [Required]
        public int RequestId { get; set; }

        [Required]
        public int MainAttachmentId { get; set; }

        [MaxLength(20)]
        public string? AssignedGNOfficerNIC { get; set; }

        [Required]
        [MaxLength(50)]
        public string VerificationStatus { get; set; } = "PENDING";

        public string? VerificationRemarks { get; set; }

        public DateTime? VerifiedDate { get; set; }

        public int? FinalVerifiedAttachmentId { get; set; }

        [MaxLength(255)]
        public string? VerificationTokenHash { get; set; }

        [MaxLength(500)]
        public string? VerificationUrl { get; set; }

        [MaxLength(255)]
        public string? OriginalFileHash { get; set; }

        [MaxLength(255)]
        public string? FinalFileHash { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        public string? RevocationReason { get; set; }
        public DateTime? RevokedDate { get; set; }

        [MaxLength(100)]
        public string? RevokedBy { get; set; }

        // Navigation properties
        [ForeignKey("RequestId")]
        public virtual UnifiedRequest? Request { get; set; }

        [ForeignKey("MainAttachmentId")]
        public virtual UnifiedRequestAttachment? MainAttachment { get; set; }

        [ForeignKey("FinalVerifiedAttachmentId")]
        public virtual UnifiedRequestAttachment? FinalVerifiedAttachment { get; set; }

        [ForeignKey("AssignedGNOfficerNIC")]
        public virtual GnOfficer? AssignedOfficer { get; set; }
    }
}
