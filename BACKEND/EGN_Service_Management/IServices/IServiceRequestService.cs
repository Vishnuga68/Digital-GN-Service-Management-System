using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IServiceRequestService
    {
        Task<IEnumerable<ServiceRequest>> GetAllAsync();
        Task<ServiceRequest> GetByIdAsync(string id);
        Task<ServiceRequest> CreateAsync(ServiceRequest entity);
        Task<ServiceRequest> UpdateAsync(string id, ServiceRequest entity);
        Task DeleteAsync(string id);
    }
}