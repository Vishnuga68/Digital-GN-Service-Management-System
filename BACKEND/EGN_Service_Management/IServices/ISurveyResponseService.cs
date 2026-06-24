using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface ISurveyResponseService
    {
        Task<IEnumerable<SurveyResponse>> GetAllAsync();
        Task<SurveyResponse> GetByIdAsync(int id);
        Task<SurveyResponse> CreateAsync(SurveyResponse entity);
        Task<SurveyResponse> UpdateAsync(int id, SurveyResponse entity);
        Task DeleteAsync(int id);
    }
}