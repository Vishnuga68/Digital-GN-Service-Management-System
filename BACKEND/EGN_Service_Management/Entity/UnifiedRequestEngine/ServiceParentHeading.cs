using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Service_Parent_Headings")]
    public class ServiceParentHeading
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ParentHeadingId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ParentHeadingName { get; set; }

        [MaxLength(255)]
        public string Icon { get; set; }

        public string Description { get; set; }

        public virtual ICollection<UnifiedService> Services { get; set; }
    }
}
