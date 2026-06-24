using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class ChatMessageService : IChatMessageService
    {
        private readonly IChatMessageRepository _repository;

        public ChatMessageService(IChatMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ChatMessage>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ChatMessage> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("ChatMessage not found.");
            }

            return entity;
        }

        public async Task<ChatMessage> CreateAsync(ChatMessage entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<ChatMessage> UpdateAsync(string id, ChatMessage entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}