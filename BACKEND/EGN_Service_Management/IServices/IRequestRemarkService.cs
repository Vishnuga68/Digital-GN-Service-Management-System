using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IRequestRemarkService
    {
        Task<IEnumerable<RequestRemark>> GetAllAsync();
        Task<RequestRemark> GetByIdAsync(int id);
        Task<RequestRemark> CreateAsync(RequestRemark entity);
        Task<RequestRemark> UpdateAsync(int id, RequestRemark entity);
        Task DeleteAsync(int id);
    }
}