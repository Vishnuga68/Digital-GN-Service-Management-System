using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EgnServiceUserController : ControllerBase
    {
        private readonly IEgnServiceUserService _egnServiceUserService;

        public EgnServiceUserController(IEgnServiceUserService egnServiceUserService)
        {
            _egnServiceUserService = egnServiceUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEgnServiceUserRecords()
        {
            var records = await _egnServiceUserService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEgnServiceUserById(string id)
        {
            try
            {
                var record = await _egnServiceUserService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "EgnServiceUser not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateEgnServiceUser([FromBody] EgnServiceUserRequestDTO requestDTO)
        {
            try
            {
                var record = await _egnServiceUserService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetEgnServiceUserById), new { id = record.Nic }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEgnServiceUser(string id, [FromBody] EgnServiceUserRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Nic = id;
                var record = await _egnServiceUserService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "EgnServiceUser not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEgnServiceUser(string id)
        {
            try
            {
                await _egnServiceUserService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "EgnServiceUser not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static EgnServiceUser MapToEntity(EgnServiceUserRequestDTO requestDTO)
        {
            return new EgnServiceUser
            {
                Nic = requestDTO.Nic,
                Username = requestDTO.Username,
                RegisteredAt = requestDTO.RegisteredAt,
            };
        }

        private static EgnServiceUserResponseDTO MapToResponseDTO(EgnServiceUser entity)
        {
            return new EgnServiceUserResponseDTO
            {
                Nic = entity.Nic,
                Username = entity.Username,
                RegisteredAt = entity.RegisteredAt,
            };
        }
    }
}
