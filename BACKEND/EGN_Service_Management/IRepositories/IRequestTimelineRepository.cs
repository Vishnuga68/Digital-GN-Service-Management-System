using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IRequestTimelineRepository
    {
        Task<IEnumerable<RequestTimeline>> GetAllAsync();
        Task<RequestTimeline?> GetByIdAsync(int id);
        Task<RequestTimeline> AddAsync(RequestTimeline entity);
        Task<RequestTimeline> UpdateAsync(int id, RequestTimeline entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}