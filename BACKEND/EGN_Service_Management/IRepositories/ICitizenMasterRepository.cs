using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface ICitizenMasterRepository
    {
        Task<IEnumerable<CitizenMaster>> GetAllAsync();
        Task<CitizenMaster?> GetByIdAsync(string id);
        Task<CitizenMaster> AddAsync(CitizenMaster entity);
        Task<CitizenMaster> UpdateAsync(string id, CitizenMaster entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}