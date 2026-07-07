using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Request_Data")]
    public class RequestData
    {
        [Key]
        public int RequestId { get; set; }

        public string JsonData { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        // Navigation property
        [ForeignKey("RequestId")]
        public virtual UnifiedRequest Request { get; set; }
    }
}
