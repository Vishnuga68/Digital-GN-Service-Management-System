using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestRemarkController : ControllerBase
    {
        private readonly IRequestRemarkService _requestRemarkService;

        public RequestRemarkController(IRequestRemarkService requestRemarkService)
        {
            _requestRemarkService = requestRemarkService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequestRemarkRecords()
        {
            var records = await _requestRemarkService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestRemarkById(int id)
        {
            try
            {
                var record = await _requestRemarkService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestRemark not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequestRemark([FromBody] RequestRemarkRequestDTO requestDTO)
        {
            try
            {
                var record = await _requestRemarkService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetRequestRemarkById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequestRemark(int id, [FromBody] RequestRemarkRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _requestRemarkService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestRemark not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequestRemark(int id)
        {
            try
            {
                await _requestRemarkService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestRemark not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static RequestRemark MapToEntity(RequestRemarkRequestDTO requestDTO)
        {
            return new RequestRemark
            {
                Id = requestDTO.Id,
                RequestId = requestDTO.RequestId,
                AuthorNic = requestDTO.AuthorNic,
                RoleLabel = requestDTO.RoleLabel,
                RemarkText = requestDTO.RemarkText,
                CreatedAt = requestDTO.CreatedAt,
            };
        }

        private static RequestRemarkResponseDTO MapToResponseDTO(RequestRemark entity)
        {
            return new RequestRemarkResponseDTO
            {
                Id = entity.Id,
                RequestId = entity.RequestId,
                AuthorNic = entity.AuthorNic,
                RoleLabel = entity.RoleLabel,
                RemarkText = entity.RemarkText,
                CreatedAt = entity.CreatedAt,
            };
        }
    }
}
