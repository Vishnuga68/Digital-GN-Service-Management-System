using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{
    [Table("Account_Password_Reset_OTP")]
    public class AccountPasswordResetOtp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string AccountNic { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string OtpHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public int AttemptCount { get; set; } = 0;

        [Required]
        public int MaxAttempts { get; set; } = 5;

        [Required]
        public bool IsVerified { get; set; } = false;

        [Required]
        public bool IsUsed { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? VerifiedAt { get; set; }

        public DateTime? UsedAt { get; set; }

        [MaxLength(50)]
        public string? CreatedIp { get; set; }

        [ForeignKey("AccountNic")]
        public virtual Account? Account { get; set; }
    }
}
