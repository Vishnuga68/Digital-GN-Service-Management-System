using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DsDivisionController : ControllerBase
    {
        private readonly IDsDivisionService _dsDivisionService;

        public DsDivisionController(IDsDivisionService dsDivisionService)
        {
            _dsDivisionService = dsDivisionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDsDivisionRecords([FromQuery] int? districtId)
        {
            var records = await _dsDivisionService.GetAllAsync();
            
            if (districtId.HasValue)
            {
                records = records.Where(r => r.DistrictId == districtId.Value);
            }
            
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{**id}")]
        public async Task<IActionResult> GetDsDivisionById(string id)
        {
            try
            {
                string decodedId = Uri.UnescapeDataString(id);
                var record = await _dsDivisionService.GetByIdAsync(decodedId);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "DsDivision not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateDsDivision([FromBody] DsDivisionRequestDTO requestDTO)
        {
            try
            {
                var record = await _dsDivisionService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetDsDivisionById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDsDivision(string id, [FromBody] DsDivisionRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _dsDivisionService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "DsDivision not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDsDivision(string id)
        {
            try
            {
                await _dsDivisionService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "DsDivision not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static DsDivision MapToEntity(DsDivisionRequestDTO requestDTO)
        {
            return new DsDivision
            {
                Id = requestDTO.Id,
                Name = requestDTO.Name,
                DistrictId = requestDTO.DistrictId,
                ProvinceId = requestDTO.ProvinceId,
            };
        }

        private static DsDivisionResponseDTO MapToResponseDTO(DsDivision entity)
        {
            return new DsDivisionResponseDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                DistrictId = entity.DistrictId,
                ProvinceId = entity.ProvinceId,
            };
        }
    }
}
