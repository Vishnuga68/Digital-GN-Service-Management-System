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

        // Navigation property
        [ForeignKey("RequestId")]
        public virtual UnifiedRequest Request { get; set; }
    }
}
