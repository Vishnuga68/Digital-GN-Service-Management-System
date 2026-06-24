using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface ITransferRequestService
    {
        Task<IEnumerable<TransferRequest>> GetAllAsync();
        Task<TransferRequest> GetByIdAsync(string id);
        Task<TransferRequest> CreateAsync(TransferRequest entity);
        Task<TransferRequest> UpdateAsync(string id, TransferRequest entity);
        Task DeleteAsync(string id);
    }
}