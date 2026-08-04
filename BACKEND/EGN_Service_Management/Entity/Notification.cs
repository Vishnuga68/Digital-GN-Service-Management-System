using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RecipientNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string RecipientRole { get; set; } = string.Empty; // "Citizen", "GNOfficer", "DSOfficer"

        [Required]
        [MaxLength(30)]
        public string Type { get; set; } = string.Empty; // "approved", "rejected", "pending", "info", "mention"

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? RelatedEntityType { get; set; } // "Request", "Appointment", "ChatMessage", "Announcement"

        [MaxLength(50)]
        public string? RelatedEntityId { get; set; }

        [MaxLength(255)]
        public string? ActionUrl { get; set; }

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Low"; // "Low", "Medium", "High"

        public bool IsRead { get; set; } = false;

        public DateTime? ReadDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? CreatedBy { get; set; } // Sender Nic or "System"

        public bool IsActive { get; set; } = true;
    }
}
