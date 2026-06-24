using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IEgnServiceUserService
    {
        Task<IEnumerable<EgnServiceUser>> GetAllAsync();
        Task<EgnServiceUser> GetByIdAsync(string id);
        Task<EgnServiceUser> CreateAsync(EgnServiceUser entity);
        Task<EgnServiceUser> UpdateAsync(string id, EgnServiceUser entity);
        Task DeleteAsync(string id);
    }
}
