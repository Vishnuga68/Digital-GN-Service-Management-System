using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IDsDivisionRepository
    {
        Task<IEnumerable<DsDivision>> GetAllAsync();
        Task<DsDivision?> GetByIdAsync(string id);
        Task<DsDivision> AddAsync(DsDivision entity);
        Task<DsDivision> UpdateAsync(string id, DsDivision entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}