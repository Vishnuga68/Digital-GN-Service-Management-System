using AutoMapper;
using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;

namespace EGN_Service_Management.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Account Mappings
            CreateMap<Account, AccountRequestDTO>().ReverseMap();
            CreateMap<Account, AccountResponseDTO>().ReverseMap();

            // Announcement Mappings
            CreateMap<Announcement, AnnouncementRequestDTO>().ReverseMap();
            CreateMap<Announcement, AnnouncementResponseDTO>().ReverseMap();

            // AnnouncementTargetCitizen Mappings
            CreateMap<AnnouncementTargetCitizen, AnnouncementTargetCitizenRequestDTO>().ReverseMap();
            CreateMap<AnnouncementTargetCitizen, AnnouncementTargetCitizenResponseDTO>().ReverseMap();

            // ChatAttachment Mappings
            CreateMap<ChatAttachment, ChatAttachmentRequestDTO>().ReverseMap();
            CreateMap<ChatAttachment, ChatAttachmentResponseDTO>().ReverseMap();

            // ChatMessage Mappings
            CreateMap<ChatMessage, ChatMessageRequestDTO>().ReverseMap();
            CreateMap<ChatMessage, ChatMessageResponseDTO>().ReverseMap();

            // CitizenMaster Mappings
            CreateMap<CitizenMaster, CitizenMasterRequestDTO>().ReverseMap();
            CreateMap<CitizenMaster, CitizenMasterResponseDTO>().ReverseMap();


            // Conversation Mappings
            CreateMap<Conversation, ConversationRequestDTO>().ReverseMap();
            CreateMap<Conversation, ConversationResponseDTO>().ReverseMap();

            // DocumentCategory Mappings
            CreateMap<DocumentCategory, DocumentCategoryRequestDTO>().ReverseMap();
            CreateMap<DocumentCategory, DocumentCategoryResponseDTO>().ReverseMap();

            // DsDivision Mappings
            CreateMap<DsDivision, DsDivisionRequestDTO>().ReverseMap();
            CreateMap<DsDivision, DsDivisionResponseDTO>().ReverseMap();

            // DsOfficer Mappings
            CreateMap<DsOfficer, DsOfficerRequestDTO>().ReverseMap();
            CreateMap<DsOfficer, DsOfficerResponseDTO>().ReverseMap();

            // EgnServiceUser Mappings
            CreateMap<EgnServiceUser, EgnServiceUserRequestDTO>().ReverseMap();
            CreateMap<EgnServiceUser, EgnServiceUserResponseDTO>().ReverseMap();

            // FamilyCard Mappings
            CreateMap<FamilyCard, FamilyCardRequestDTO>().ReverseMap();
            CreateMap<FamilyCard, FamilyCardResponseDTO>().ReverseMap();

            // GnDivision Mappings
            CreateMap<GnDivision, GnDivisionRequestDTO>().ReverseMap();
            CreateMap<GnDivision, GnDivisionResponseDTO>().ReverseMap();

            // GnOfficer Mappings
            CreateMap<GnOfficer, GnOfficerRequestDTO>().ReverseMap();
            CreateMap<GnOfficer, GnOfficerResponseDTO>().ReverseMap();

            // Notification Mappings
            CreateMap<Notification, NotificationRequestDTO>().ReverseMap();
            CreateMap<Notification, NotificationResponseDTO>().ReverseMap();

            // PollOption Mappings
            CreateMap<PollOption, PollOptionRequestDTO>().ReverseMap();
            CreateMap<PollOption, PollOptionResponseDTO>().ReverseMap();

            // PollVote Mappings
            CreateMap<PollVote, PollVoteRequestDTO>().ReverseMap();
            CreateMap<PollVote, PollVoteResponseDTO>().ReverseMap();

            // RequestAttachment Mappings
            CreateMap<RequestAttachment, RequestAttachmentRequestDTO>().ReverseMap();
            CreateMap<RequestAttachment, RequestAttachmentResponseDTO>().ReverseMap();

            // RequestRemark Mappings
            CreateMap<RequestRemark, RequestRemarkRequestDTO>().ReverseMap();
            CreateMap<RequestRemark, RequestRemarkResponseDTO>().ReverseMap();

            // RequestTimeline Mappings
            CreateMap<RequestTimeline, RequestTimelineRequestDTO>().ReverseMap();
            CreateMap<RequestTimeline, RequestTimelineResponseDTO>().ReverseMap();

            // ServiceRequest Mappings
            CreateMap<ServiceRequest, ServiceRequestRequestDTO>().ReverseMap();
            CreateMap<ServiceRequest, ServiceRequestResponseDTO>().ReverseMap();

            // SurveyResponse Mappings
            CreateMap<SurveyResponse, SurveyResponseRequestDTO>().ReverseMap();
            CreateMap<SurveyResponse, SurveyResponseResponseDTO>().ReverseMap();

            // TransferRequest Mappings
            CreateMap<TransferRequest, TransferRequestRequestDTO>().ReverseMap();
            CreateMap<TransferRequest, TransferRequestResponseDTO>().ReverseMap();
        }
    }
}
