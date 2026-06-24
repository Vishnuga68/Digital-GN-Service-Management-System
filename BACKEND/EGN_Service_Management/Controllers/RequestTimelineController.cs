using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestTimelineController : ControllerBase
    {
        private readonly IRequestTimelineService _requestTimelineService;

        public RequestTimelineController(IRequestTimelineService requestTimelineService)
        {
            _requestTimelineService = requestTimelineService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequestTimelineRecords()
        {
            var records = await _requestTimelineService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestTimelineById(int id)
        {
            try
            {
                var record = await _requestTimelineService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestTimeline not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequestTimeline([FromBody] RequestTimelineRequestDTO requestDTO)
        {
            try
            {
                var record = await _requestTimelineService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetRequestTimelineById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequestTimeline(int id, [FromBody] RequestTimelineRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _requestTimelineService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestTimeline not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequestTimeline(int id)
        {
            try
            {
                await _requestTimelineService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "RequestTimeline not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static RequestTimeline MapToEntity(RequestTimelineRequestDTO requestDTO)
        {
            return new RequestTimeline
            {
                Id = requestDTO.Id,
                RequestId = requestDTO.RequestId,
                Step = requestDTO.Step,
                Title = requestDTO.Title,
                Timestamp = requestDTO.Timestamp,
                Completed = requestDTO.Completed,
                OfficerNic = requestDTO.OfficerNic,
            };
        }

        private static RequestTimelineResponseDTO MapToResponseDTO(RequestTimeline entity)
        {
            return new RequestTimelineResponseDTO
            {
                Id = entity.Id,
                RequestId = entity.RequestId,
                Step = entity.Step,
                Title = entity.Title,
                Timestamp = entity.Timestamp,
                Completed = entity.Completed,
                OfficerNic = entity.OfficerNic,
            };
        }
    }
}
