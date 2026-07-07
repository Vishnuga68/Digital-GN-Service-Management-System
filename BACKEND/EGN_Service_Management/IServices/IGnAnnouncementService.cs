using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IGnAnnouncementService
    {
        Task<IEnumerable<GnAnnouncementResponseDTO>> GetGnAnnouncementsAsync(string divisionId);
        Task<IEnumerable<GnAnnouncementResponseDTO>> GetCitizenAnnouncementsAsync(string divisionId, string? citizenNic = null);
        Task<GnAnnouncementResponseDTO> GetByIdAsync(int id, bool isCitizen, string? citizenNic = null);
        
        Task<GnAnnouncementResponseDTO> CreateAsync(GnAnnouncementRequestDTO request, string gnOfficerNic, string divisionId);
        Task<GnAnnouncementResponseDTO> UpdateAsync(int id, GnAnnouncementRequestDTO request, string gnOfficerNic);
        Task DeleteAsync(int id, string gnOfficerNic);
        
        Task SubmitVoteAsync(int announcementId, int optionId, string citizenNic);
        
        Task<IEnumerable<GNDocument>> GetPublishedDocumentsAsync(string gnOfficerNic);
        Task<IEnumerable<GNDocument>> GetPublishedDocumentsByDivisionAsync(string divisionId);
    }
}
