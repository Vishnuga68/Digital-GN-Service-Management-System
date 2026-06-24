using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IGnDivisionRepository
    {
        Task<IEnumerable<GnDivision>> GetAllAsync();
        Task<GnDivision?> GetByIdAsync(string id);
        Task<GnDivision> AddAsync(GnDivision entity);
        Task<GnDivision> UpdateAsync(string id, GnDivision entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}