using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Request_Attachments")]
    public class UnifiedRequestAttachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttachmentId { get; set; }

        [Required]
        public int RequestId { get; set; }

        [Required]
        [MaxLength(500)]
        public string FileName { get; set; }

        [Required]
        public string FileUrl { get; set; }

        [MaxLength(100)]
        public string FileType { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; }

        [MaxLength(50)]
        public string? DocumentRole { get; set; }

        [MaxLength(255)]
        public string? DisplayName { get; set; }

        public bool IsPrimary { get; set; } = false;

        [MaxLength(100)]
        public string? UploadedBy { get; set; }

        public long? FileSize { get; set; }

        [MaxLength(100)]
        public string? MimeType { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("RequestId")]
        public virtual UnifiedRequest Request { get; set; }
    }
}
