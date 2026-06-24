using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IGnOfficerService
    {
        Task<IEnumerable<GnOfficer>> GetAllAsync();
        Task<GnOfficer> GetByIdAsync(string id);
        Task<GnOfficer> CreateAsync(GnOfficer entity);
        Task<GnOfficer> UpdateAsync(string id, GnOfficer entity);
        Task DeleteAsync(string id);
    }
}