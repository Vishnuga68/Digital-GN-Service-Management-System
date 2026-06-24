using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IAnnouncementTargetCitizenService
    {
        Task<IEnumerable<AnnouncementTargetCitizen>> GetAllAsync();
        Task<AnnouncementTargetCitizen> GetByIdAsync(int id);
        Task<AnnouncementTargetCitizen> CreateAsync(AnnouncementTargetCitizen entity);
        Task<AnnouncementTargetCitizen> UpdateAsync(int id, AnnouncementTargetCitizen entity);
        Task DeleteAsync(int id);
    }
}