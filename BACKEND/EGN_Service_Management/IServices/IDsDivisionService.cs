using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IDsDivisionService
    {
        Task<IEnumerable<DsDivision>> GetAllAsync();
        Task<DsDivision> GetByIdAsync(string id);
        Task<DsDivision> CreateAsync(DsDivision entity);
        Task<DsDivision> UpdateAsync(string id, DsDivision entity);
        Task DeleteAsync(string id);
    }
}