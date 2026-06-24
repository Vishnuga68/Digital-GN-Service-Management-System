using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DsOfficerController : ControllerBase
    {
        private readonly IDsOfficerService _dsOfficerService;

        public DsOfficerController(IDsOfficerService dsOfficerService)
        {
            _dsOfficerService = dsOfficerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDsOfficerRecords()
        {
            var records = await _dsOfficerService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDsOfficerById(string id)
        {
            try
            {
                var record = await _dsOfficerService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "DsOfficer not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateDsOfficer([FromBody] DsOfficerRequestDTO requestDTO)
        {
            try
            {
                var record = await _dsOfficerService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetDsOfficerById), new { id = record.Nic }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDsOfficer(string id, [FromBody] DsOfficerRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Nic = id;
                var record = await _dsOfficerService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "DsOfficer not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDsOfficer(string id)
        {
            try
            {
                await _dsOfficerService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "DsOfficer not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static DsOfficer MapToEntity(DsOfficerRequestDTO requestDTO)
        {
            return new DsOfficer
            {
                Nic = requestDTO.Nic,
                FullName = requestDTO.FullName,
                DsDivisionId = requestDTO.DsDivisionId,
            };
        }

        private static DsOfficerResponseDTO MapToResponseDTO(DsOfficer entity)
        {
            return new DsOfficerResponseDTO
            {
                Nic = entity.Nic,
                FullName = entity.FullName,
                DsDivisionId = entity.DsDivisionId,
            };
        }
    }
}
