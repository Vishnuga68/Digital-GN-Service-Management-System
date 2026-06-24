using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllNotificationRecords()
        {
            var records = await _notificationService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotificationById(int id)
        {
            try
            {
                var record = await _notificationService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Notification not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] NotificationRequestDTO requestDTO)
        {
            try
            {
                var record = await _notificationService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetNotificationById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotification(int id, [FromBody] NotificationRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _notificationService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Notification not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                await _notificationService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Notification not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static Notification MapToEntity(NotificationRequestDTO requestDTO)
        {
            return new Notification
            {
                Id = requestDTO.Id,
                RecipientNic = requestDTO.RecipientNic,
                Type = requestDTO.Type,
                Title = requestDTO.Title,
                Message = requestDTO.Message,
                IsRead = requestDTO.IsRead,
                CreatedAt = requestDTO.CreatedAt,
            };
        }

        private static NotificationResponseDTO MapToResponseDTO(Notification entity)
        {
            return new NotificationResponseDTO
            {
                Id = entity.Id,
                RecipientNic = entity.RecipientNic,
                Type = entity.Type,
                Title = entity.Title,
                Message = entity.Message,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt,
            };
        }
    }
}
