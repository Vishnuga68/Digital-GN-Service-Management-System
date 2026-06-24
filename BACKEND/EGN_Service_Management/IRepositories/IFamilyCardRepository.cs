using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IFamilyCardRepository
    {
        Task<IEnumerable<FamilyCard>> GetAllAsync();
        Task<FamilyCard?> GetByIdAsync(string id);
        Task<FamilyCard> AddAsync(FamilyCard entity);
        Task<FamilyCard> UpdateAsync(string id, FamilyCard entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}