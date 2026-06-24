using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface IGNDocumentRepository
    {
        Task<IEnumerable<GNDocument>> GetByDivisionIdAsync(string divisionId, string? status = null, int? categoryId = null);
        Task<GNDocument?> GetByIdAsync(int id);
        Task<GNDocument> AddAsync(GNDocument entity);
        Task UpdateAsync(GNDocument entity);
        Task DeleteAsync(GNDocument entity);
        Task AddLogAsync(GNDocumentLog log);
    }
}
