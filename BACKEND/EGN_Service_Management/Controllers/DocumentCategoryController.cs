using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentCategoryController : ControllerBase
    {
        private readonly IDocumentCategoryService _service;

        public DocumentCategoryController(IDocumentCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentCategoryResponseDTO>>> GetAll()
        {
            var categories = await _service.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentCategoryResponseDTO>> GetById(int id)
        {
            var category = await _service.GetByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,DsOfficer,DSOfficer,GnOfficer,GNOfficer")]
        public async Task<ActionResult<DocumentCategoryResponseDTO>> Create([FromBody] DocumentCategoryRequestDTO dto)
        {
            var createdCategory = await _service.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdCategory.CategoryId }, createdCategory);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,DsOfficer,DSOfficer,GnOfficer,GNOfficer")]
        public async Task<IActionResult> Update(int id, [FromBody] DocumentCategoryRequestDTO dto)
        {
            await _service.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,DsOfficer,DSOfficer,GnOfficer,GNOfficer")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
