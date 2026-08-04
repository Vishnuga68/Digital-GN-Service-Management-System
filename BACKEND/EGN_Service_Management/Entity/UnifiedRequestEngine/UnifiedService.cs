using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Services")]
    public class UnifiedService
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServiceId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ServiceName { get; set; }

        public int? ParentHeadingId { get; set; }

        [MaxLength(255)]
        public string? Icon { get; set; }

        public string? Description { get; set; }

        [ForeignKey("ParentHeadingId")]
        public virtual ServiceParentHeading ParentHeading { get; set; }

        public virtual ICollection<ServiceCategory> Categories { get; set; }
        public virtual ICollection<UnifiedRequest> Requests { get; set; }
    }
}
