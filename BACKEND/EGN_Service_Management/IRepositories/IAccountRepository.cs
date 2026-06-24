using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IAccountRepository
    {
        Task<IEnumerable<Account>> GetAllAsync();
        Task<Account?> GetByIdAsync(string id);
        Task<Account> AddAsync(Account entity);
        Task<Account> UpdateAsync(string id, Account entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}