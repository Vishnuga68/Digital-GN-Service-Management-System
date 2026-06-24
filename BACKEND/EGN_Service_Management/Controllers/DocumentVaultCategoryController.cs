using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EGN_Service_Management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentVaultCategoryController : ControllerBase
    {
        private readonly IDocumentVaultCategoryService _service;

        public DocumentVaultCategoryController(IDocumentVaultCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var nic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleId = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(nic) || string.IsNullOrEmpty(roleId))
                return Unauthorized("Missing user credentials in token.");

            var categories = await _service.GetCategoriesAsync(nic, roleId);
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategories([FromBody] VaultCategoryRequestDTO requestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var nic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleId = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(nic) || string.IsNullOrEmpty(roleId))
                return Unauthorized("Missing user credentials in token.");

            var updated = await _service.UpdateCategoriesAsync(requestDto, nic, roleId);
            return Ok(updated);
        }
    }
}
