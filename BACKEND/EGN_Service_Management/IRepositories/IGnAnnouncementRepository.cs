using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IGnAnnouncementRepository
    {
        Task<IEnumerable<GnAnnouncement>> GetAnnouncementsByDivisionAsync(string divisionId, bool publishedOnly = false);
        Task<GnAnnouncement?> GetByIdAsync(int id);
        Task<GnAnnouncement> CreateAsync(GnAnnouncement announcement);
        Task<GnAnnouncement> UpdateAsync(GnAnnouncement announcement);
        Task DeleteAsync(GnAnnouncement announcement);
        
        Task<GnPollOption> CreatePollOptionAsync(GnPollOption option);
        Task DeletePollOptionsAsync(int announcementId);
        
        Task<bool> HasCitizenVotedAsync(int announcementId, string citizenNic);
        Task<GnPollVote> SubmitVoteAsync(GnPollVote vote);
        
        Task<IEnumerable<GNDocument>> GetPublishedDocumentsByGnOfficerAsync(string gnOfficerNic);
        Task<IEnumerable<GNDocument>> GetPublishedDocumentsByDivisionAsync(string divisionId);
        Task<string> GetFullNameByNicAsync(string nic);
    }
}
