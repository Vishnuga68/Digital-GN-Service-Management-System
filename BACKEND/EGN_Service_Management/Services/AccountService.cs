using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;

        public AccountService(IAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Account> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Account not found.");
            }

            return entity;
        }

        public async Task<Account> CreateAsync(Account entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<Account> UpdateAsync(string id, Account entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}