using AutoMapper;
using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class DocumentCategoryService : IDocumentCategoryService
    {
        private readonly IDocumentCategoryRepository _repository;
        private readonly IMapper _mapper;

        public DocumentCategoryService(IDocumentCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DocumentCategoryResponseDTO>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<DocumentCategoryResponseDTO>>(entities);
        }

        public async Task<DocumentCategoryResponseDTO?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<DocumentCategoryResponseDTO>(entity);
        }

        public async Task<DocumentCategoryResponseDTO> AddAsync(DocumentCategoryRequestDTO dto)
        {
            var entity = _mapper.Map<DocumentCategory>(dto);
            var createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<DocumentCategoryResponseDTO>(createdEntity);
        }

        public async Task UpdateAsync(int id, DocumentCategoryRequestDTO dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null)
            {
                entity.CategoryName = dto.CategoryName;
                entity.IsActive = dto.IsActive;
                await _repository.UpdateAsync(entity);
            }
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}