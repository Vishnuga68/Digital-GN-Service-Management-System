namespace EGN_Service_Management.DTO.ResponseDTO
{
    public class GnAnnouncementResponseDTO
    {
        public int AnnouncementId { get; set; }
        public string GnOfficerNic { get; set; } = string.Empty;
        public string GnDivisionId { get; set; } = string.Empty;
        public string AnnouncementType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DocumentId { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentUrl { get; set; }
        public string? ExternalSurveyUrl { get; set; }
        public string? PollQuestion { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? ModifiedByName { get; set; }
        public string Status { get; set; } = string.Empty; // Draft, Published, Expired
        
        public bool HasVoted { get; set; }
        public int? UserVotedOptionId { get; set; }

        public List<GnPollOptionResponseDTO> PollOptions { get; set; } = new List<GnPollOptionResponseDTO>();
    }

    public class GnPollOptionResponseDTO
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int Votes { get; set; }
    }
}
