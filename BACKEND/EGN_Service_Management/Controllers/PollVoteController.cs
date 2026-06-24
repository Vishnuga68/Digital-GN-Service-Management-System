using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollVoteController : ControllerBase
    {
        private readonly IPollVoteService _pollVoteService;

        public PollVoteController(IPollVoteService pollVoteService)
        {
            _pollVoteService = pollVoteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPollVoteRecords()
        {
            var records = await _pollVoteService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPollVoteById(int id)
        {
            try
            {
                var record = await _pollVoteService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "PollVote not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePollVote([FromBody] PollVoteRequestDTO requestDTO)
        {
            try
            {
                var record = await _pollVoteService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetPollVoteById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePollVote(int id, [FromBody] PollVoteRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _pollVoteService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "PollVote not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePollVote(int id)
        {
            try
            {
                await _pollVoteService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "PollVote not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static PollVote MapToEntity(PollVoteRequestDTO requestDTO)
        {
            return new PollVote
            {
                Id = requestDTO.Id,
                AnnouncementId = requestDTO.AnnouncementId,
                CitizenNic = requestDTO.CitizenNic,
                PollOptionId = requestDTO.PollOptionId,
                CreatedAt = requestDTO.CreatedAt,
            };
        }

        private static PollVoteResponseDTO MapToResponseDTO(PollVote entity)
        {
            return new PollVoteResponseDTO
            {
                Id = entity.Id,
                AnnouncementId = entity.AnnouncementId,
                CitizenNic = entity.CitizenNic,
                PollOptionId = entity.PollOptionId,
                CreatedAt = entity.CreatedAt,
            };
        }
    }
}
