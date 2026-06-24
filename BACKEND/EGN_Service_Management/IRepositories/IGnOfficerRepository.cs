using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IGnOfficerRepository
    {
        Task<IEnumerable<GnOfficer>> GetAllAsync();
        Task<GnOfficer?> GetByIdAsync(string id);
        Task<GnOfficer> AddAsync(GnOfficer entity);
        Task<GnOfficer> UpdateAsync(string id, GnOfficer entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}