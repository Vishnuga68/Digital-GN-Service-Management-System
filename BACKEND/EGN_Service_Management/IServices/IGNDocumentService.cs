using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using Microsoft.AspNetCore.Http;

namespace EGN_Service_Management.IServices
{
    public interface IGNDocumentService
    {
        Task<IEnumerable<GNDocumentResponseDTO>> GetDocumentsAsync(string divisionId, string? status, int? categoryId);
        Task<GNDocumentResponseDTO?> GetDocumentByIdAsync(int id);
        Task<GNDocumentResponseDTO> CreateDocumentAsync(GNDocumentRequestDTO request, IFormFile file, string officerNic, string divisionId);
        Task<GNDocumentResponseDTO> UpdateDocumentAsync(int id, GNDocumentRequestDTO request, IFormFile? file, string officerNic);
        Task PublishDocumentAsync(int id, string officerNic);
        Task DeleteDocumentAsync(int id, string officerNic);
    }
}
