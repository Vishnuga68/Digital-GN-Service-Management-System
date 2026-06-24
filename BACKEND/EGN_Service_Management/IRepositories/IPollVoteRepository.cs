using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IPollVoteRepository
    {
        Task<IEnumerable<PollVote>> GetAllAsync();
        Task<PollVote?> GetByIdAsync(int id);
        Task<PollVote> AddAsync(PollVote entity);
        Task<PollVote> UpdateAsync(int id, PollVote entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}