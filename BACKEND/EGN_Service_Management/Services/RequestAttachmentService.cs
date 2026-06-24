using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class RequestAttachmentService : IRequestAttachmentService
    {
        private readonly IRequestAttachmentRepository _repository;

        public RequestAttachmentService(IRequestAttachmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<RequestAttachment>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<RequestAttachment> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("RequestAttachment not found.");
            }

            return entity;
        }

        public async Task<RequestAttachment> CreateAsync(RequestAttachment entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<RequestAttachment> UpdateAsync(string id, RequestAttachment entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}