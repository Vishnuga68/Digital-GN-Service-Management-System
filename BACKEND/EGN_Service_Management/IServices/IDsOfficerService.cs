using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IDsOfficerService
    {
        Task<IEnumerable<DsOfficer>> GetAllAsync();
        Task<DsOfficer> GetByIdAsync(string id);
        Task<DsOfficer> CreateAsync(DsOfficer entity);
        Task<DsOfficer> UpdateAsync(string id, DsOfficer entity);
        Task DeleteAsync(string id);
    }
}