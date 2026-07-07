namespace EGN_Service_Management.DTO.RequestDTO
{
    public class GnAnnouncementRequestDTO
    {
        public string AnnouncementType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DocumentId { get; set; }
        public string? ExternalSurveyUrl { get; set; }
        public string? PollQuestion { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        
        public List<string>? PollOptions { get; set; }
    }
}
