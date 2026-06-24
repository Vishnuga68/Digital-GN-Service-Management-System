using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IAccountService
    {
        Task<IEnumerable<Account>> GetAllAsync();
        Task<Account> GetByIdAsync(string id);
        Task<Account> CreateAsync(Account entity);
        Task<Account> UpdateAsync(string id, Account entity);
        Task DeleteAsync(string id);
    }
}