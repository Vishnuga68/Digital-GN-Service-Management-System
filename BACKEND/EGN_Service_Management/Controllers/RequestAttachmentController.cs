using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestAttachmentController : ControllerBase
    {
        private readonly IRequestAttachmentService _requestAttachmentService;

        public RequestAttachmentController(IRequestAttachmentService requestAttachmentService)
        {
            _requestAttachmentService = requestAttachmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequestAttachmentRecords()
        {
            var records = await _requestAttachmentService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestAttachmentById(string id)
        {
            try
            {
                var record = await _requestAttachmentService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestAttachment not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequestAttachment([FromBody] RequestAttachmentRequestDTO requestDTO)
        {
            try
            {
                var record = await _requestAttachmentService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetRequestAttachmentById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequestAttachment(string id, [FromBody] RequestAttachmentRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _requestAttachmentService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestAttachment not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequestAttachment(string id)
        {
            try
            {
                await _requestAttachmentService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestAttachment not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static RequestAttachment MapToEntity(RequestAttachmentRequestDTO requestDTO)
        {
            return new RequestAttachment
            {
                Id = requestDTO.Id,
                RequestId = requestDTO.RequestId,
                FileName = requestDTO.FileName,
                FileType = requestDTO.FileType,
                FileSize = requestDTO.FileSize,
                UploadedAt = requestDTO.UploadedAt,
            };
        }

        private static RequestAttachmentResponseDTO MapToResponseDTO(RequestAttachment entity)
        {
            return new RequestAttachmentResponseDTO
            {
                Id = entity.Id,
                RequestId = entity.RequestId,
                FileName = entity.FileName,
                FileType = entity.FileType,
                FileSize = entity.FileSize,
                UploadedAt = entity.UploadedAt,
            };
        }
    }
}
