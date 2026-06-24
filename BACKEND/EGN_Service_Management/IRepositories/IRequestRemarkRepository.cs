using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IRequestRemarkRepository
    {
        Task<IEnumerable<RequestRemark>> GetAllAsync();
        Task<RequestRemark?> GetByIdAsync(int id);
        Task<RequestRemark> AddAsync(RequestRemark entity);
        Task<RequestRemark> UpdateAsync(int id, RequestRemark entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}