using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class RequestRemarkService : IRequestRemarkService
    {
        private readonly IRequestRemarkRepository _repository;

        public RequestRemarkService(IRequestRemarkRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<RequestRemark>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<RequestRemark> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("RequestRemark not found.");
            }

            return entity;
        }

        public async Task<RequestRemark> CreateAsync(RequestRemark entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<RequestRemark> UpdateAsync(int id, RequestRemark entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}