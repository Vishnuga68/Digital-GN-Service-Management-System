using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class RequestTimelineService : IRequestTimelineService
    {
        private readonly IRequestTimelineRepository _repository;

        public RequestTimelineService(IRequestTimelineRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<RequestTimeline>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<RequestTimeline> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("RequestTimeline not found.");
            }

            return entity;
        }

        public async Task<RequestTimeline> CreateAsync(RequestTimeline entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<RequestTimeline> UpdateAsync(int id, RequestTimeline entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}