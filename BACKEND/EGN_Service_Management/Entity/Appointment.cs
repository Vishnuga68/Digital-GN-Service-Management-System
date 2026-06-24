using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("appointments")]
    public class Appointment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AppointmentId { get; set; }

        [Required]
        [MaxLength(20)]
        public string CitizenNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string GnOfficerNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ServiceType { get; set; } = string.Empty;

        public DateOnly AppointmentDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "BOOKED";

        public string? CancellationReason { get; set; }

        public int? RescheduledFrom { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public EgnServiceUser? Citizen { get; set; }
        public GnOfficer? GnOfficer { get; set; }
    }
}
