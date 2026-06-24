using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IPollVoteService
    {
        Task<IEnumerable<PollVote>> GetAllAsync();
        Task<PollVote> GetByIdAsync(int id);
        Task<PollVote> CreateAsync(PollVote entity);
        Task<PollVote> UpdateAsync(int id, PollVote entity);
        Task DeleteAsync(int id);
    }
}