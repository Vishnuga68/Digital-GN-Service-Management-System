using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("document_vault_categories")]
    public class DocumentVaultCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string NIC { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleId { get; set; }

        [Required]
        public string CategoriesJson { get; set; }
    }
}
