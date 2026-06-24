using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IServiceRequestRepository
    {
        Task<IEnumerable<ServiceRequest>> GetAllAsync();
        Task<ServiceRequest?> GetByIdAsync(string id);
        Task<ServiceRequest> AddAsync(ServiceRequest entity);
        Task<ServiceRequest> UpdateAsync(string id, ServiceRequest entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}