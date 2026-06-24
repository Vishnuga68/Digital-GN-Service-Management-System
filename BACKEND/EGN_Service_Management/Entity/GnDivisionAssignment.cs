using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("gn_division_assignments")]
    public class GnDivisionAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GnOfficerNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string GnDivisionId { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // 'Active' or 'Inactive'

        [ForeignKey("GnOfficerNic")]
        public GnOfficer? GnOfficer { get; set; }

        [ForeignKey("GnDivisionId")]
        public GnDivision? GnDivision { get; set; }
    }
}
