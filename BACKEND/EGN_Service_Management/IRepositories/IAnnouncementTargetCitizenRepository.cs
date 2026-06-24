using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IAnnouncementTargetCitizenRepository
    {
        Task<IEnumerable<AnnouncementTargetCitizen>> GetAllAsync();
        Task<AnnouncementTargetCitizen?> GetByIdAsync(int id);
        Task<AnnouncementTargetCitizen> AddAsync(AnnouncementTargetCitizen entity);
        Task<AnnouncementTargetCitizen> UpdateAsync(int id, AnnouncementTargetCitizen entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}