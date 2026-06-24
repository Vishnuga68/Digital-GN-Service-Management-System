using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IGnDivisionService
    {
        Task<IEnumerable<GnDivision>> GetAllAsync();
        Task<GnDivision> GetByIdAsync(string id);
        Task<GnDivision> CreateAsync(GnDivision entity);
        Task<GnDivision> UpdateAsync(string id, GnDivision entity);
        Task DeleteAsync(string id);
    }
}