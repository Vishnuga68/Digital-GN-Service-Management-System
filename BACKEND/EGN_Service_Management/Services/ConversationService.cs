using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _repository;

        public ConversationService(IConversationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Conversation>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Conversation> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Conversation not found.");
            }

            return entity;
        }

        public async Task<Conversation> CreateAsync(Conversation entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<Conversation> UpdateAsync(string id, Conversation entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}