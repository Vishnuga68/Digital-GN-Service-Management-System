using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ConversationController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllConversationRecords()
        {
            var records = await _conversationService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetConversationById(string id)
        {
            try
            {
                var record = await _conversationService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Conversation not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] ConversationRequestDTO requestDTO)
        {
            try
            {
                var record = await _conversationService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetConversationById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConversation(string id, [FromBody] ConversationRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _conversationService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Conversation not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConversation(string id)
        {
            try
            {
                await _conversationService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Conversation not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static Conversation MapToEntity(ConversationRequestDTO requestDTO)
        {
            return new Conversation
            {
                Id = requestDTO.Id,
                CitizenNic = requestDTO.CitizenNic,
                GnOfficerNic = requestDTO.GnOfficerNic,
                RequestId = requestDTO.RequestId,
                CreatedAt = requestDTO.CreatedAt,
                UpdatedAt = requestDTO.UpdatedAt,
            };
        }

        private static ConversationResponseDTO MapToResponseDTO(Conversation entity)
        {
            return new ConversationResponseDTO
            {
                Id = entity.Id,
                CitizenNic = entity.CitizenNic,
                GnOfficerNic = entity.GnOfficerNic,
                RequestId = entity.RequestId,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }
    }
}
