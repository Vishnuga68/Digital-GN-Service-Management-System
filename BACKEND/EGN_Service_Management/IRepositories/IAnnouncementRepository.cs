using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IAnnouncementRepository
    {
        Task<IEnumerable<Announcement>> GetAllAsync();
        Task<Announcement?> GetByIdAsync(string id);
        Task<Announcement> AddAsync(Announcement entity);
        Task<Announcement> UpdateAsync(string id, Announcement entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}