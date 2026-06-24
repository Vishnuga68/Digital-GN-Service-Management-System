using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IRequestTimelineService
    {
        Task<IEnumerable<RequestTimeline>> GetAllAsync();
        Task<RequestTimeline> GetByIdAsync(int id);
        Task<RequestTimeline> CreateAsync(RequestTimeline entity);
        Task<RequestTimeline> UpdateAsync(int id, RequestTimeline entity);
        Task DeleteAsync(int id);
    }
}