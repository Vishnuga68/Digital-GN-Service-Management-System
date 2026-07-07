using System;
using System.Collections.Generic;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class RequestDetailsResponseDTO
    {
        public string RequestId { get; set; }
        public string ServiceType { get; set; }
        public string Category { get; set; }
        public string SubmittedDate { get; set; }
        public string LastUpdated { get; set; }
        public string CurrentStatus { get; set; }
        public string ApprovedDate { get; set; }
        public string RejectedDate { get; set; }
        
        public ApplicantDTO Applicant { get; set; }
        
        public List<AttachmentDTO> Attachments { get; set; }
        
        public List<DynamicFieldDTO> DynamicFields { get; set; }

        public List<RemarkDTO> Remarks { get; set; }
        
        public List<TimelineStepDTO> Timeline { get; set; }
    }

    public class ApplicantDTO
    {
        public string FullName { get; set; }
        public string Nic { get; set; }
        public string Address { get; set; }
    }

    public class AttachmentDTO
    {
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileSize { get; set; } // Will store as string (e.g. "1.2 MB") or calculate from file if needed. But DB just has URL.
        public DateTime UploadedDate { get; set; }
        public string FileType { get; set; }
    }

    public class DynamicFieldDTO
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public class RemarkDTO
    {
        public string Author { get; set; }
        public string Role { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }
    
    public class TimelineStepDTO
    {
        public int Step { get; set; }
        public string Title { get; set; }
        public DateTime? Timestamp { get; set; }
        public bool Completed { get; set; }
        public string Officer { get; set; }
    }
}
