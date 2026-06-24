using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EGN_Service_Management.IServices
{
    public interface IDocumentVaultService
    {
        Task<IEnumerable<DocumentVaultResponseDTO>> GetDocumentsAsync(string nic, string roleId);
        Task<DocumentVaultResponseDTO> UploadDocumentAsync(DocumentVaultRequestDTO requestDto, string nic, string roleId, int uploadedBy);
        Task<bool> DeleteDocumentAsync(int documentId, string nic, string roleId, int deletedBy);
    }
}
