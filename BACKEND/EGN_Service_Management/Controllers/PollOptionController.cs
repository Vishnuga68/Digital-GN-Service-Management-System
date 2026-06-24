using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollOptionController : ControllerBase
    {
        private readonly IPollOptionService _pollOptionService;

        public PollOptionController(IPollOptionService pollOptionService)
        {
            _pollOptionService = pollOptionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPollOptionRecords()
        {
            var records = await _pollOptionService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPollOptionById(string id)
        {
            try
            {
                var record = await _pollOptionService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "PollOption not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePollOption([FromBody] PollOptionRequestDTO requestDTO)
        {
            try
            {
                var record = await _pollOptionService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetPollOptionById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePollOption(string id, [FromBody] PollOptionRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _pollOptionService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "PollOption not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePollOption(string id)
        {
            try
            {
                await _pollOptionService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "PollOption not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static PollOption MapToEntity(PollOptionRequestDTO requestDTO)
        {
            return new PollOption
            {
                Id = requestDTO.Id,
                AnnouncementId = requestDTO.AnnouncementId,
                OptionText = requestDTO.OptionText,
            };
        }

        private static PollOptionResponseDTO MapToResponseDTO(PollOption entity)
        {
            return new PollOptionResponseDTO
            {
                Id = entity.Id,
                AnnouncementId = entity.AnnouncementId,
                OptionText = entity.OptionText,
            };
        }
    }
}
