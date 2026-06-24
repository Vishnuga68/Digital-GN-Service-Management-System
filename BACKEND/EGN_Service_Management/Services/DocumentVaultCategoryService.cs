using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace EGN_Service_Management.Services
{
    public class DocumentVaultCategoryService : IDocumentVaultCategoryService
    {
        private readonly IDocumentVaultCategoryRepository _repository;

        public DocumentVaultCategoryService(IDocumentVaultCategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<VaultCategoryResponseDTO> GetCategoriesAsync(string nic, string roleId)
        {
            var entity = await _repository.GetCategoriesAsync(nic, roleId);
            if (entity == null || string.IsNullOrEmpty(entity.CategoriesJson))
            {
                return new VaultCategoryResponseDTO();
            }

            return new VaultCategoryResponseDTO
            {
                Categories = JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(entity.CategoriesJson) ?? new System.Collections.Generic.List<string>()
            };
        }

        public async Task<VaultCategoryResponseDTO> UpdateCategoriesAsync(VaultCategoryRequestDTO requestDto, string nic, string roleId)
        {
            var json = JsonSerializer.Serialize(requestDto.Categories ?? new System.Collections.Generic.List<string>());

            var entity = await _repository.GetCategoriesAsync(nic, roleId);
            if (entity == null)
            {
                entity = new DocumentVaultCategory
                {
                    NIC = nic,
                    RoleId = roleId,
                    CategoriesJson = json
                };
                await _repository.AddCategoriesAsync(entity);
            }
            else
            {
                entity.CategoriesJson = json;
                await _repository.UpdateCategoriesAsync(entity);
            }

            return new VaultCategoryResponseDTO { Categories = requestDto.Categories ?? new System.Collections.Generic.List<string>() };
        }
    }
}
