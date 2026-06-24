using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IRepositories
{
    public interface ISurveyResponseRepository
    {
        Task<IEnumerable<SurveyResponse>> GetAllAsync();
        Task<SurveyResponse?> GetByIdAsync(int id);
        Task<SurveyResponse> AddAsync(SurveyResponse entity);
        Task<SurveyResponse> UpdateAsync(int id, SurveyResponse entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}