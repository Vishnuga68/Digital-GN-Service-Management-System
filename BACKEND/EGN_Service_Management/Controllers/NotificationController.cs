using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userNic) || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized();
            }

            var records = await _notificationService.GetUserNotificationsAsync(userNic, userRole);
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userNic) || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized();
            }

            var count = await _notificationService.GetUnreadCountAsync(userNic, userRole);
            return Ok(new { count });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id);
                return Ok(new { success = true, message = "Notification marked as read." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userNic) || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized();
            }

            try
            {
                await _notificationService.MarkAllAsReadAsync(userNic, userRole);
                return Ok(new { success = true, message = "All notifications marked as read." });
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

        private static NotificationResponseDTO MapToResponseDTO(Notification entity)
        {
            return new NotificationResponseDTO
            {
                Id = entity.Id,
                RecipientNic = entity.RecipientNic,
                RecipientRole = entity.RecipientRole,
                Type = entity.Type,
                Title = entity.Title,
                Message = entity.Message,
                RelatedEntityType = entity.RelatedEntityType,
                RelatedEntityId = entity.RelatedEntityId,
                ActionUrl = entity.ActionUrl,
                Priority = entity.Priority,
                IsRead = entity.IsRead,
                ReadDate = entity.ReadDate,
                CreatedAt = entity.CreatedAt,
                CreatedBy = entity.CreatedBy,
                IsActive = entity.IsActive
            };
        }
    }
}
