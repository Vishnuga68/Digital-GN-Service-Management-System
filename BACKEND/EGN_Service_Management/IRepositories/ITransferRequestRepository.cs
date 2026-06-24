using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface ITransferRequestRepository
    {
        Task<IEnumerable<TransferRequest>> GetAllAsync();
        Task<TransferRequest?> GetByIdAsync(string id);
        Task<TransferRequest> AddAsync(TransferRequest entity);
        Task<TransferRequest> UpdateAsync(string id, TransferRequest entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}