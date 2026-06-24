using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IAnnouncementService
    {
        Task<IEnumerable<Announcement>> GetAllAsync();
        Task<Announcement> GetByIdAsync(string id);
        Task<Announcement> CreateAsync(Announcement entity);
        Task<Announcement> UpdateAsync(string id, Announcement entity);
        Task DeleteAsync(string id);
    }
}