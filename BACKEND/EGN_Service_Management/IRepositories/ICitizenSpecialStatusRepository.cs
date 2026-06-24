using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface ICitizenSpecialStatusRepository
    {
        Task<IEnumerable<CitizenSpecialStatus>> GetAllAsync();
        Task<CitizenSpecialStatus?> GetByIdAsync(string id);
        Task<CitizenSpecialStatus> AddAsync(CitizenSpecialStatus entity);
        Task<CitizenSpecialStatus> UpdateAsync(string id, CitizenSpecialStatus entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}