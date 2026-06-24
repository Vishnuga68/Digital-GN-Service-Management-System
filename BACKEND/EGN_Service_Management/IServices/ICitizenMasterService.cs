using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface ICitizenMasterService
    {
        Task<IEnumerable<CitizenMaster>> GetAllAsync();
        Task<CitizenMaster> GetByIdAsync(string id);
        Task<CitizenMaster> CreateAsync(CitizenMaster entity);
        Task<CitizenMaster> UpdateAsync(string id, CitizenMaster entity);
        Task DeleteAsync(string id);
    }
}