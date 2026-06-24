using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IEgnServiceUserRepository
    {
        Task<IEnumerable<EgnServiceUser>> GetAllAsync();
        Task<EgnServiceUser?> GetByIdAsync(string id);
        Task<EgnServiceUser> AddAsync(EgnServiceUser entity);
        Task<EgnServiceUser> UpdateAsync(string id, EgnServiceUser entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}
