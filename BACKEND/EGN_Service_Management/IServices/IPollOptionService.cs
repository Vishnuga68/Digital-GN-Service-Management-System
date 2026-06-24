using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IPollOptionService
    {
        Task<IEnumerable<PollOption>> GetAllAsync();
        Task<PollOption> GetByIdAsync(string id);
        Task<PollOption> CreateAsync(PollOption entity);
        Task<PollOption> UpdateAsync(string id, PollOption entity);
        Task DeleteAsync(string id);
    }
}