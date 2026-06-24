using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IFamilyCardService
    {
        Task<IEnumerable<FamilyCard>> GetAllAsync();
        Task<FamilyCard> GetByIdAsync(string id);
        Task<FamilyCard> CreateAsync(FamilyCard entity);
        Task<FamilyCard> UpdateAsync(string id, FamilyCard entity);
        Task DeleteAsync(string id);
    }
}