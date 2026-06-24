using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("survey_responses")]
public class SurveyResponse
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string AnnouncementId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CitizenNic { get; set; } = string.Empty;

    [Required]
    public string ResponseText { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Announcement? Announcement { get; set; }
    public EgnServiceUser? Citizen { get; set; }
}
}
