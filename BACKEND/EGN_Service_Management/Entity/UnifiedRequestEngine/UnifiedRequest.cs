using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Requests")]
    public class UnifiedRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }

        [Required]
        [MaxLength(30)]
        public string RequestNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CurrentStatus { get; set; }

        [Required]
        public DateTime SubmittedDate { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        [MaxLength(20)]
        public string? AssignedGnOfficerNic { get; set; }

        [MaxLength(100)]
        public string? GnDivision { get; set; }

        public string? FinalDocumentUrl { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }

        // Navigation properties
        [ForeignKey("CitizenNic")]
        public virtual CitizenMaster Citizen { get; set; }

        [ForeignKey("ServiceId")]
        public virtual UnifiedService Service { get; set; }

        [ForeignKey("CategoryId")]
        public virtual ServiceCategory Category { get; set; }

        [ForeignKey("AssignedGnOfficerNic")]
        public virtual GnOfficer AssignedOfficer { get; set; }

        // Related data
        public virtual RequestData DynamicData { get; set; }
        public virtual ICollection<UnifiedRequestAttachment> Attachments { get; set; }
        public virtual ICollection<RequestStatusHistory> StatusHistories { get; set; } = new List<RequestStatusHistory>();
        public virtual ICollection<UnifiedRequestRemark> Remarks { get; set; }
    }
}
