using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class PollVoteService : IPollVoteService
    {
        private readonly IPollVoteRepository _repository;

        public PollVoteService(IPollVoteRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PollVote>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<PollVote> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("PollVote not found.");
            }

            return entity;
        }

        public async Task<PollVote> CreateAsync(PollVote entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<PollVote> UpdateAsync(int id, PollVote entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}