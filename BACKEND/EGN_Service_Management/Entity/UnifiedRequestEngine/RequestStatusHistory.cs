using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Request_Status_History")]
    public class RequestStatusHistory
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        public string Remarks { get; set; }

        [Required]
        [MaxLength(20)]
        public string ChangedBy { get; set; }

        [Required]
        public DateTime ChangedDate { get; set; }

        // Navigation properties
        [ForeignKey("RequestId")]
        public virtual UnifiedRequest Request { get; set; }

        [ForeignKey("ChangedBy")]
        public virtual Account ChangedByAccount { get; set; }
    }
}
