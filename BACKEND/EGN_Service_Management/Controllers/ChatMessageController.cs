using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatMessageController : ControllerBase
    {
        private readonly IChatMessageService _chatMessageService;

        public ChatMessageController(IChatMessageService chatMessageService)
        {
            _chatMessageService = chatMessageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChatMessageRecords()
        {
            var records = await _chatMessageService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChatMessageById(string id)
        {
            try
            {
                var record = await _chatMessageService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ChatMessage not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChatMessage([FromBody] ChatMessageRequestDTO requestDTO)
        {
            try
            {
                var record = await _chatMessageService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetChatMessageById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChatMessage(string id, [FromBody] ChatMessageRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _chatMessageService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ChatMessage not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatMessage(string id)
        {
            try
            {
                await _chatMessageService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ChatMessage not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static ChatMessage MapToEntity(ChatMessageRequestDTO requestDTO)
        {
            return new ChatMessage
            {
                Id = requestDTO.Id,
                ConversationId = requestDTO.ConversationId,
                SenderNic = requestDTO.SenderNic,
                MessageText = requestDTO.MessageText,
                IsRead = requestDTO.IsRead,
                Status = requestDTO.Status,
                CreatedAt = requestDTO.CreatedAt,
            };
        }

        private static ChatMessageResponseDTO MapToResponseDTO(ChatMessage entity)
        {
            return new ChatMessageResponseDTO
            {
                Id = entity.Id,
                ConversationId = entity.ConversationId,
                SenderNic = entity.SenderNic,
                MessageText = entity.MessageText,
                IsRead = entity.IsRead,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
            };
        }
    }
}
