using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class TransferRequestService : ITransferRequestService
    {
        private readonly ITransferRequestRepository _repository;

        public TransferRequestService(ITransferRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TransferRequest>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<TransferRequest> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("TransferRequest not found.");
            }

            return entity;
        }

        public async Task<TransferRequest> CreateAsync(TransferRequest entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<TransferRequest> UpdateAsync(string id, TransferRequest entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}