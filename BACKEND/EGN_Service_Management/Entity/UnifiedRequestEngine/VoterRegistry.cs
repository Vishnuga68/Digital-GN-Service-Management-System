using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Voter_Registry")]
    public class VoterRegistry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; }

        [Required]
        public DateTime RegisteredDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string RegisteredByGnOfficer { get; set; }

        [Required]
        public bool IsExported { get; set; }

        // Navigation properties
        [ForeignKey("CitizenNic")]
        public virtual CitizenMaster? Citizen { get; set; }

        [ForeignKey("RegisteredByGnOfficer")]
        public virtual GnOfficer? RegisteringOfficer { get; set; }
    }
}
