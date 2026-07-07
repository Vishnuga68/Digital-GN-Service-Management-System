using EGN_Service_Management.DTO;
using System.Threading.Tasks;

namespace EGN_Service_Management.IServices
{
    public interface ICitizenRequestService
    {
        Task<string> SubmitRequestAsync(CitizenRequestSubmissionDTO requestDto, string currentUserId);
    }
}
