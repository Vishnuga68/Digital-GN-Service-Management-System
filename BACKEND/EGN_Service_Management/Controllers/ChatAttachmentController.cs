using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatAttachmentController : ControllerBase
    {
        private readonly IChatAttachmentService _chatAttachmentService;

        public ChatAttachmentController(IChatAttachmentService chatAttachmentService)
        {
            _chatAttachmentService = chatAttachmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChatAttachmentRecords()
        {
            var records = await _chatAttachmentService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChatAttachmentById(string id)
        {
            try
            {
                var record = await _chatAttachmentService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ChatAttachment not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChatAttachment([FromBody] ChatAttachmentRequestDTO requestDTO)
        {
            try
            {
                var record = await _chatAttachmentService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetChatAttachmentById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChatAttachment(string id, [FromBody] ChatAttachmentRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _chatAttachmentService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ChatAttachment not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatAttachment(string id)
        {
            try
            {
                await _chatAttachmentService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ChatAttachment not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static ChatAttachment MapToEntity(ChatAttachmentRequestDTO requestDTO)
        {
            return new ChatAttachment
            {
                Id = requestDTO.Id,
                MessageId = requestDTO.MessageId,
                FileName = requestDTO.FileName,
                FileType = requestDTO.FileType,
                FileUrl = requestDTO.FileUrl,
            };
        }

        private static ChatAttachmentResponseDTO MapToResponseDTO(ChatAttachment entity)
        {
            return new ChatAttachmentResponseDTO
            {
                Id = entity.Id,
                MessageId = entity.MessageId,
                FileName = entity.FileName,
                FileType = entity.FileType,
                FileUrl = entity.FileUrl,
            };
        }
    }
}
