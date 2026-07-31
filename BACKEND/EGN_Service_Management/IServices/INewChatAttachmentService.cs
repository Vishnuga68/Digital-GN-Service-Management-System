using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EGN_Service_Management.DTO;

namespace EGN_Service_Management.IServices
{
    public interface INewChatAttachmentService
    {
        Task<ChatAttachmentDTO> UploadAttachmentAsync(int conversationId, int messageId, string uploadedByNic, IFormFile file, bool isSupportingDocument);
        Task<bool> AddAttachmentToRequestAsync(int attachmentId, string gnOfficerNic);
    }
}
