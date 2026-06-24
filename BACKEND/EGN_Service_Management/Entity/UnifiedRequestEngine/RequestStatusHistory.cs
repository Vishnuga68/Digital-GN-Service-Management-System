using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Request_Status_History")]
    public class RequestStatusHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
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
