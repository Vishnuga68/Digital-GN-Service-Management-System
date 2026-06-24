using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GnDivisionController : ControllerBase
    {
        private readonly IGnDivisionService _gnDivisionService;

        public GnDivisionController(IGnDivisionService gnDivisionService)
        {
            _gnDivisionService = gnDivisionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGnDivisionRecords([FromQuery] string? dsDivisionId)
        {
            var records = await _gnDivisionService.GetAllAsync();
            
            if (!string.IsNullOrEmpty(dsDivisionId))
            {
                records = records.Where(r => r.DsDivisionId == dsDivisionId);
            }
            
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{**id}")]
        public async Task<IActionResult> GetGnDivisionById(string id)
        {
            try
            {
                string decodedId = Uri.UnescapeDataString(id);
                var record = await _gnDivisionService.GetByIdAsync(decodedId);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "GnDivision not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateGnDivision([FromBody] GnDivisionRequestDTO requestDTO)
        {
            try
            {
                var record = await _gnDivisionService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetGnDivisionById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGnDivision(string id, [FromBody] GnDivisionRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _gnDivisionService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "GnDivision not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGnDivision(string id)
        {
            try
            {
                await _gnDivisionService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "GnDivision not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static GnDivision MapToEntity(GnDivisionRequestDTO requestDTO)
        {
            return new GnDivision
            {
                Id = requestDTO.Id,
                Name = requestDTO.Name,
                DsDivisionId = requestDTO.DsDivisionId,
            };
        }

        private static GnDivisionResponseDTO MapToResponseDTO(GnDivision entity)
        {
            return new GnDivisionResponseDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                DsDivisionId = entity.DsDivisionId,
            };
        }
    }
}
