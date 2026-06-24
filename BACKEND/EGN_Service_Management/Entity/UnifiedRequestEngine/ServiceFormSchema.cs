using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Service_Form_Schema")]
    public class ServiceFormSchema
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FieldName { get; set; }

        [Required]
        [MaxLength(255)]
        public string FieldLabel { get; set; }

        [Required]
        [MaxLength(50)]
        public string FieldType { get; set; }

        public string? DropdownOptions { get; set; }

        public bool IsRequired { get; set; }

        public string? ValidationRules { get; set; }

        public int DisplayOrder { get; set; }

        [MaxLength(100)]
        public string? FormSection { get; set; }

        [MaxLength(50)]
        public string FormVersion { get; set; } = "1.0";

        public bool IsActive { get; set; } = true;

        [ForeignKey("CategoryId")]
        public virtual ServiceCategory Category { get; set; }
    }
}
