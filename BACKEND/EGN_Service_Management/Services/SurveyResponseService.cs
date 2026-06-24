using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class SurveyResponseService : ISurveyResponseService
    {
        private readonly ISurveyResponseRepository _repository;

        public SurveyResponseService(ISurveyResponseRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SurveyResponse>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<SurveyResponse> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("SurveyResponse not found.");
            }

            return entity;
        }

        public async Task<SurveyResponse> CreateAsync(SurveyResponse entity)
        {
            return await _repository.AddAsync(entity);
        }

        public async Task<SurveyResponse> UpdateAsync(int id, SurveyResponse entity)
        {
            return await _repository.UpdateAsync(id, entity);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}