using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Family_Member_History")]
    public class FamilyMemberHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; }

        [Required]
        [MaxLength(50)]
        public string FamilyCardNumber { get; set; }

        public DateTime? JoinedDate { get; set; }
        public DateTime? LeftDate { get; set; }

        public string Reason { get; set; }

        // Navigation properties
        [ForeignKey("CitizenNic")]
        public virtual CitizenMaster Citizen { get; set; }

        [ForeignKey("FamilyCardNumber")]
        public virtual FamilyCard FamilyCard { get; set; }
    }
}
