using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGN_Service_Management.Entity
{

[Table("EGN_Service_Users")]
public class EgnServiceUser
{
    [Key]
    [MaxLength(20)]
    public string Nic { get; set; } = string.Empty;

    [NotMapped]
    public string Username { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public CitizenMaster? CitizenMaster { get; set; }
    public Account? Account { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();
    public ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
}
