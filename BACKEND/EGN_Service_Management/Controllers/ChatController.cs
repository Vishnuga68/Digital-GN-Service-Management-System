using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EGN_Service_Management.DTO;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly INewChatAttachmentService _attachmentService;
        private readonly EGN_Service_Management.Database.EgnDbContext _context;
        private readonly INotificationService _notificationService;

        public ChatController(IChatService chatService, INewChatAttachmentService attachmentService, EGN_Service_Management.Database.EgnDbContext context, INotificationService notificationService)
        {
            _chatService = chatService;
            _attachmentService = attachmentService;
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet("common")]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> GetOrCreateCommonConversation()
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(callerNic))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));
            }

            try
            {
                var conversation = await _chatService.GetOrCreateCommonConversationAsync(callerNic, callerNic);
                return Ok(ApiResponse<ChatConversationDTO>.SuccessResponse(conversation, "Common conversation retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("request/{requestId}")]
        public async Task<IActionResult> GetOrCreateRequestConversation(int requestId)
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(callerNic))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));
            }

            try
            {
                var conversation = await _chatService.GetOrCreateRequestConversationAsync(requestId, callerNic);
                return Ok(ApiResponse<ChatConversationDTO>.SuccessResponse(conversation, "Request support conversation retrieved successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(callerNic) || string.IsNullOrEmpty(role))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));
            }

            try
            {
                var list = await _chatService.GetConversationsForUserAsync(callerNic, role);
                return Ok(ApiResponse<IEnumerable<ChatConversationDTO>>.SuccessResponse(list, "Conversations retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(callerNic))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));
            }

            try
            {
                var messages = await _chatService.GetMessagesAsync(conversationId, callerNic, page, pageSize);
                return Ok(ApiResponse<IEnumerable<ChatMessageDTO>>.SuccessResponse(messages, "Messages retrieved successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("conversations/{conversationId}/messages")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendMessageDTO requestDto)
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(callerNic) || string.IsNullOrEmpty(role))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token."));
            }

            try
            {
                var message = await _chatService.SendMessageAsync(conversationId, callerNic, role, requestDto.MessageText ?? string.Empty);

                var conversation = await _context.ChatConversations.FindAsync(conversationId);
                if (conversation != null)
                {
                    string recipientNic = string.Equals(callerNic, conversation.CitizenNic, StringComparison.OrdinalIgnoreCase) 
                        ? conversation.GnOfficerNic 
                        : conversation.CitizenNic;
                    string recipientRole = string.Equals(callerNic, conversation.CitizenNic, StringComparison.OrdinalIgnoreCase) 
                        ? "GNOfficer" 
                        : "Citizen";
                    string senderName = string.Equals(callerNic, conversation.CitizenNic, StringComparison.OrdinalIgnoreCase) 
                        ? "Citizen" 
                        : "GN Officer";

                    await _notificationService.SendNotificationAsync(
                        recipientNic: recipientNic,
                        recipientRole: recipientRole,
                        type: "chat",
                        title: $"New Message from {senderName}",
                        message: requestDto.MessageText ?? "Sent an attachment.",
                        relatedEntityType: "Chat",
                        relatedEntityId: conversationId.ToString(),
                        actionUrl: recipientRole == "Citizen" ? "/citizen/chat" : "/gn-officer/chat",
                        priority: "Medium",
                        createdBy: callerNic
                    );
                }

                return Ok(ApiResponse<ChatMessageDTO>.SuccessResponse(message, "Message sent successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        [HttpPut("conversations/{conversationId}/read")]
        public async Task<IActionResult> MarkAsRead(int conversationId)
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(callerNic))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token."));
            }

            try
            {
                var unreadCount = await _chatService.MarkMessagesAsReadAsync(conversationId, callerNic);
                return Ok(ApiResponse<int>.SuccessResponse(unreadCount, "Messages marked as read successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(callerNic) || string.IsNullOrEmpty(role))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token."));
            }

            try
            {
                var countDto = await _chatService.GetUnreadCountsAsync(callerNic, role);
                return Ok(ApiResponse<ChatUnreadCountDTO>.SuccessResponse(countDto, "Unread count retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("conversations/{conversationId}/messages/{messageId}/attachments")]
        public async Task<IActionResult> UploadAttachment(int conversationId, int messageId, [FromForm] ChatAttachmentUploadDTO dto, [FromQuery] bool isSupportingDocument = false)
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(callerNic))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token."));
            }

            try
            {
                var attachment = await _attachmentService.UploadAttachmentAsync(conversationId, messageId, callerNic, dto.File, isSupportingDocument);
                return Ok(ApiResponse<ChatAttachmentDTO>.SuccessResponse(attachment, "Attachment uploaded successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("attachments/{attachmentId}/add-to-request")]
        [Authorize(Roles = "GNOfficer,GnOfficer")]
        public async Task<IActionResult> AddAttachmentToRequest(int attachmentId)
        {
            var callerNic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(callerNic))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token."));
            }

            try
            {
                var success = await _attachmentService.AddAttachmentToRequestAsync(attachmentId, callerNic);
                if (success)
                {
                    return Ok(ApiResponse<object>.SuccessResponse("Attachment successfully added to request documents."));
                }
                return BadRequest(ApiResponse<object>.ErrorResponse("Failed to add attachment to request."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }
    }
}
