using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GnAnnouncementsController : ControllerBase
    {
        private readonly IGnAnnouncementService _service;
        private readonly EGN_Service_Management.Database.EgnDbContext _context;

        public GnAnnouncementsController(IGnAnnouncementService service, EGN_Service_Management.Database.EgnDbContext context)
        {
            _service = service;
            _context = context;
        }

        private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        
        private async Task<string> GetGnDivisionIdAsync()
        {
            var claim = User.FindFirst("GnDivisionId")?.Value;
            if (!string.IsNullOrEmpty(claim)) return claim;

            var nic = GetUserId();
            if (string.IsNullOrEmpty(nic)) return string.Empty;

            var role = GetUserRole();
            if (role == "Citizen")
            {
                var citizen = await _context.CitizenMasters.FindAsync(nic);
                return citizen?.GnDivisionId ?? string.Empty;
            }

            var officer = await _context.GnOfficers.FindAsync(nic);
            return officer?.GnDivisionId ?? string.Empty;
        }

        [HttpGet("gn")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> GetGnAnnouncements()
        {
            var divisionId = await GetGnDivisionIdAsync();
            if (string.IsNullOrEmpty(divisionId)) return Forbid();

            var result = await _service.GetGnAnnouncementsAsync(divisionId);
            return Ok(result);
        }

        [HttpGet("citizen")]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> GetCitizenAnnouncements()
        {
            var divisionId = await GetGnDivisionIdAsync();
            if (string.IsNullOrEmpty(divisionId)) return Forbid();

            var nic = GetUserId();
            var result = await _service.GetCitizenAnnouncementsAsync(divisionId, nic);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var role = GetUserRole();
            bool isCitizen = role == "Citizen";
            var nic = GetUserId();

            try
            {
                var result = await _service.GetByIdAsync(id, isCitizen, nic);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> Create([FromBody] GnAnnouncementRequestDTO request)
        {
            var nic = GetUserId();
            var divisionId = await GetGnDivisionIdAsync();

            if (string.IsNullOrEmpty(nic) || string.IsNullOrEmpty(divisionId)) return Forbid();

            var result = await _service.CreateAsync(request, nic, divisionId);
            return CreatedAtAction(nameof(GetById), new { id = result.AnnouncementId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> Update(int id, [FromBody] GnAnnouncementRequestDTO request)
        {
            var nic = GetUserId();
            try
            {
                var result = await _service.UpdateAsync(id, request, nic);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> Delete(int id)
        {
            var nic = GetUserId();
            try
            {
                await _service.DeleteAsync(id, nic);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id}/vote")]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> SubmitVote(int id, [FromBody] GnPollVoteRequestDTO request)
        {
            var nic = GetUserId();
            try
            {
                await _service.SubmitVoteAsync(id, request.OptionId, nic);
                return Ok(new { message = "Vote submitted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{id}/results")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> GetResults(int id)
        {
            try
            {
                var announcement = await _service.GetByIdAsync(id, isCitizen: false);
                if (announcement.AnnouncementType != "Poll")
                {
                    return BadRequest("Not a poll.");
                }

                var results = announcement.PollOptions.Select(po => new
                {
                    option = po.OptionText,
                    votes = po.Votes
                });

                return Ok(results);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("/api/documents/published/by-division")]
        [HttpGet("/api/documents/published/by-division/{divisionId}")]
        [Authorize(Roles = "GNOfficer,GnOfficer")]
        public async Task<IActionResult> GetPublishedDocumentsByDivision(string? divisionId = null)
        {
            // Get division from DB for the logged-in GN
            var jwtDivisionId = await GetGnDivisionIdAsync();
            if (string.IsNullOrEmpty(jwtDivisionId)) return Forbid();

            // Ignore 'divisionId' from route, use 'jwtDivisionId'
            var documents = await _service.GetPublishedDocumentsByDivisionAsync(jwtDivisionId);
            
            var result = documents.Select(d => new
            {
                d.DocumentId,
                d.Title,
                d.FileName
            });
            return Ok(result);
        }
    }
}
