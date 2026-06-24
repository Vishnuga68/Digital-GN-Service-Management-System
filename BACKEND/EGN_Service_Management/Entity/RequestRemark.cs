using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("request_remarks")]
public class RequestRemark
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string AuthorNic { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RoleLabel { get; set; } = string.Empty;

    [Required]
    public string RemarkText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ServiceRequest? ServiceRequest { get; set; }
    public Account? Author { get; set; }
}
}
