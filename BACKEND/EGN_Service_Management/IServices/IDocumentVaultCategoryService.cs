using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using System.Threading.Tasks;

namespace EGN_Service_Management.IServices
{
    public interface IDocumentVaultCategoryService
    {
        Task<VaultCategoryResponseDTO> GetCategoriesAsync(string nic, string roleId);
        Task<VaultCategoryResponseDTO> UpdateCategoriesAsync(VaultCategoryRequestDTO requestDto, string nic, string roleId);
    }
}
