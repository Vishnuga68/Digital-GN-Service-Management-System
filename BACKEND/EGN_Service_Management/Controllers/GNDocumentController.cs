using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GNDocumentController : ControllerBase
    {
        private readonly IGNDocumentService _service;
        private readonly IGnOfficerService _gnOfficerService;
        private readonly ICitizenMasterService _citizenMasterService;

        public GNDocumentController(IGNDocumentService service, IGnOfficerService gnOfficerService, ICitizenMasterService citizenMasterService)
        {
            _service = service;
            _gnOfficerService = gnOfficerService;
            _citizenMasterService = citizenMasterService;
        }

        private string GetNicFromToken()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        private async Task<string> GetGnDivisionIdForUser()
        {
            var nic = GetNicFromToken();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "GnOfficer" || role == "GNOfficer")
            {
                var officer = await _gnOfficerService.GetByIdAsync(nic);
                if (officer != null && !string.IsNullOrEmpty(officer.GnDivisionId))
                {
                    return officer.GnDivisionId;
                }
            }
            else if (role == "Citizen")
            {
                var citizen = await _citizenMasterService.GetByIdAsync(nic);
                if (citizen != null && !string.IsNullOrEmpty(citizen.GnDivisionId))
                {
                    return citizen.GnDivisionId;
                }
            }

            return string.Empty;
        }

        [HttpGet]
        [Authorize(Roles = "GNOfficer,GnOfficer,Citizen")]
        public async Task<ActionResult<IEnumerable<GNDocumentResponseDTO>>> GetDocuments([FromQuery] string? status, [FromQuery] int? categoryId)
        {
            var divisionId = await GetGnDivisionIdForUser();
            if (string.IsNullOrEmpty(divisionId)) return BadRequest("Division ID not found for user");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Citizen")
            {
                // Citizens can only see Published documents
                status = "Published";
            }

            var documents = await _service.GetDocumentsAsync(divisionId, status, categoryId);
            return Ok(documents);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "GNOfficer,GnOfficer,Citizen")]
        public async Task<ActionResult<GNDocumentResponseDTO>> GetDocumentById(int id)
        {
            var divisionId = await GetGnDivisionIdForUser();
            var document = await _service.GetDocumentByIdAsync(id);

            if (document == null) return NotFound();
            if (document.DivisionId != divisionId) return Forbid("You can only access documents in your GN division.");
            
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Citizen" && document.Status != "Published")
            {
                return Forbid("Citizens can only access published documents.");
            }

            return Ok(document);
        }

        [HttpPost]
        [Authorize(Roles = "GNOfficer,GnOfficer")]
        public async Task<ActionResult<GNDocumentResponseDTO>> CreateDocument(
            [FromForm] GNDocumentRequestDTO request, 
            IFormFile file)
        {
            var nic = GetNicFromToken();
            var divisionId = await GetGnDivisionIdForUser();

            if (string.IsNullOrEmpty(divisionId)) return BadRequest("GN Division not found for officer.");
            if (file == null || file.Length == 0) return BadRequest("File is required.");

            var createdDoc = await _service.CreateDocumentAsync(request, file, nic, divisionId);
            return CreatedAtAction(nameof(GetDocumentById), new { id = createdDoc.DocumentId }, createdDoc);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "GNOfficer,GnOfficer")]
        public async Task<ActionResult<GNDocumentResponseDTO>> UpdateDocument(
            int id, 
            [FromForm] GNDocumentRequestDTO request, 
            IFormFile? file)
        {
            var nic = GetNicFromToken();
            
            try
            {
                var updatedDoc = await _service.UpdateDocumentAsync(id, request, file, nic);
                return Ok(updatedDoc);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{id}/publish")]
        [Authorize(Roles = "GNOfficer,GnOfficer")]
        public async Task<IActionResult> PublishDocument(int id)
        {
            var nic = GetNicFromToken();
            try
            {
                await _service.PublishDocumentAsync(id, nic);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "GNOfficer,GnOfficer")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var nic = GetNicFromToken();
            try
            {
                await _service.DeleteDocumentAsync(id, nic);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
