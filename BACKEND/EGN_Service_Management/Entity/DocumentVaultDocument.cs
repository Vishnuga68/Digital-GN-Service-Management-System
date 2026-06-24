using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("document_vault_documents")]
    public class DocumentVaultDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VaultDocumentId { get; set; }

        [Required]
        [MaxLength(20)]
        public string NIC { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("roleId")]
        public string RoleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; }

        [Required]
        [MaxLength(255)]
        public string DocumentName { get; set; }

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }

        [MaxLength(50)]
        public string FileType { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int UploadedBy { get; set; }
    }
}
