using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IDsOfficerRepository
    {
        Task<IEnumerable<DsOfficer>> GetAllAsync();
        Task<DsOfficer?> GetByIdAsync(string id);
        Task<DsOfficer> AddAsync(DsOfficer entity);
        Task<DsOfficer> UpdateAsync(string id, DsOfficer entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}