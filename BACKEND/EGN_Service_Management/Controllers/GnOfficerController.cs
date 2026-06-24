using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GnOfficerController : ControllerBase
    {
        private readonly IGnOfficerService _gnOfficerService;

        public GnOfficerController(IGnOfficerService gnOfficerService)
        {
            _gnOfficerService = gnOfficerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGnOfficerRecords()
        {
            var records = await _gnOfficerService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGnOfficerById(string id)
        {
            try
            {
                var record = await _gnOfficerService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "GnOfficer not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateGnOfficer([FromBody] GnOfficerRequestDTO requestDTO)
        {
            try
            {
                var record = await _gnOfficerService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetGnOfficerById), new { id = record.Nic }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGnOfficer(string id, [FromBody] GnOfficerRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Nic = id;
                var record = await _gnOfficerService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "GnOfficer not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGnOfficer(string id)
        {
            try
            {
                await _gnOfficerService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "GnOfficer not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static GnOfficer MapToEntity(GnOfficerRequestDTO requestDTO)
        {
            return new GnOfficer
            {
                Nic = requestDTO.Nic,
                FullName = requestDTO.FullName,
                GnDivisionId = requestDTO.GnDivisionId,
            };
        }

        private static GnOfficerResponseDTO MapToResponseDTO(GnOfficer entity)
        {
            return new GnOfficerResponseDTO
            {
                Nic = entity.Nic,
                FullName = entity.FullName,
                GnDivisionId = entity.GnDivisionId,
            };
        }
    }
}
