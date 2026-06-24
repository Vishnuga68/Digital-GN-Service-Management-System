using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;

namespace EGN_Service_Management.IServices
{
    public interface IDocumentCategoryService
    {
        Task<IEnumerable<DocumentCategoryResponseDTO>> GetAllAsync();
        Task<DocumentCategoryResponseDTO?> GetByIdAsync(int id);
        Task<DocumentCategoryResponseDTO> AddAsync(DocumentCategoryRequestDTO dto);
        Task UpdateAsync(int id, DocumentCategoryRequestDTO dto);
        Task DeleteAsync(int id);
    }
}