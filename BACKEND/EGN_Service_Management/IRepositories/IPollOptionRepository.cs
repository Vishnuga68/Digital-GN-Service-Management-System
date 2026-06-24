using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IPollOptionRepository
    {
        Task<IEnumerable<PollOption>> GetAllAsync();
        Task<PollOption?> GetByIdAsync(string id);
        Task<PollOption> AddAsync(PollOption entity);
        Task<PollOption> UpdateAsync(string id, PollOption entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}