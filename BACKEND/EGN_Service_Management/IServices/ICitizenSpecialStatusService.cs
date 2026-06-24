using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface ICitizenSpecialStatusService
    {
        Task<IEnumerable<CitizenSpecialStatus>> GetAllAsync();
        Task<CitizenSpecialStatus> GetByIdAsync(string id);
        Task<CitizenSpecialStatus> CreateAsync(CitizenSpecialStatus entity);
        Task<CitizenSpecialStatus> UpdateAsync(string id, CitizenSpecialStatus entity);
        Task DeleteAsync(string id);
    }
}