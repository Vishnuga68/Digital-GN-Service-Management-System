using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitizenSpecialStatusController : ControllerBase
    {
        private readonly ICitizenSpecialStatusService _citizenSpecialStatusService;

        public CitizenSpecialStatusController(ICitizenSpecialStatusService citizenSpecialStatusService)
        {
            _citizenSpecialStatusService = citizenSpecialStatusService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCitizenSpecialStatusRecords()
        {
            var records = await _citizenSpecialStatusService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCitizenSpecialStatusById(string id)
        {
            try
            {
                var record = await _citizenSpecialStatusService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "CitizenSpecialStatus not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCitizenSpecialStatus([FromBody] CitizenSpecialStatusRequestDTO requestDTO)
        {
            try
            {
                var record = await _citizenSpecialStatusService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetCitizenSpecialStatusById), new { id = record.CitizenNic }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCitizenSpecialStatus(string id, [FromBody] CitizenSpecialStatusRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.CitizenNic = id;
                var record = await _citizenSpecialStatusService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "CitizenSpecialStatus not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCitizenSpecialStatus(string id)
        {
            try
            {
                await _citizenSpecialStatusService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "CitizenSpecialStatus not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static CitizenSpecialStatus MapToEntity(CitizenSpecialStatusRequestDTO requestDTO)
        {
            return new CitizenSpecialStatus
            {
                CitizenNic = requestDTO.CitizenNic,
                IsNalanpuri = requestDTO.IsNalanpuri,
                HasKidneyDisease = requestDTO.HasKidneyDisease,
                HasCancer = requestDTO.HasCancer,
                HasElderlyEligible = requestDTO.HasElderlyEligible,
                WelfareDetails = requestDTO.WelfareDetails,
            };
        }

        private static CitizenSpecialStatusResponseDTO MapToResponseDTO(CitizenSpecialStatus entity)
        {
            return new CitizenSpecialStatusResponseDTO
            {
                CitizenNic = entity.CitizenNic,
                IsNalanpuri = entity.IsNalanpuri,
                HasKidneyDisease = entity.HasKidneyDisease,
                HasCancer = entity.HasCancer,
                HasElderlyEligible = entity.HasElderlyEligible,
                WelfareDetails = entity.WelfareDetails,
            };
        }
    }
}
