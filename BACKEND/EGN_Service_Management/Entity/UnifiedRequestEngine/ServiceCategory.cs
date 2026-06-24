using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Service_Categories")]
    public class ServiceCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        [MaxLength(255)]
        public string CategoryName { get; set; }

        public string GuidelineText { get; set; }

        // Navigation properties
        [ForeignKey("ServiceId")]
        public virtual UnifiedService Service { get; set; }

        public virtual ICollection<UnifiedRequest> Requests { get; set; }
        public virtual ICollection<ServiceFormSchema> FormSchemas { get; set; }
    }
}
