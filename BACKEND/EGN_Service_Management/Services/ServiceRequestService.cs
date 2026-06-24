using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class ServiceRequestService : IServiceRequestService
    {
        private readonly IServiceRequestRepository _repository;

        public ServiceRequestService(IServiceRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ServiceRequest>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ServiceRequest> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("ServiceRequest not found.");
            }

            return entity;
        }

        public async Task<ServiceRequest> CreateAsync(ServiceRequest entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<ServiceRequest> UpdateAsync(string id, ServiceRequest entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}