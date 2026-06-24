using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Request_Remarks")]
    public class UnifiedRequestRemark
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int RequestId { get; set; }

        [Required]
        [MaxLength(20)]
        public string OfficerNic { get; set; }

        [Required]
        public string Remark { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        [ForeignKey("RequestId")]
        public virtual UnifiedRequest Request { get; set; }

        [ForeignKey("OfficerNic")]
        public virtual GnOfficer Officer { get; set; }
    }
}
