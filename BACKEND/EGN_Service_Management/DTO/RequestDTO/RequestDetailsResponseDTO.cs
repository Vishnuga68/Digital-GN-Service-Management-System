using System;
using System.Collections.Generic;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class RequestDetailsResponseDTO
    {
        public string RequestId { get; set; }
        public string ServiceType { get; set; }
        public string Category { get; set; }
        public int CategoryId { get; set; }
        public string SubmittedDate { get; set; }
        public string LastUpdated { get; set; }
        public string CurrentStatus { get; set; }
        public string ApprovedDate { get; set; }
        public string RejectedDate { get; set; }
        public string AssignedOfficerNic { get; set; }
        public string? GnDivision { get; set; }
        
        public ApplicantDTO Applicant { get; set; }
        
        public List<AttachmentDTO> Attachments { get; set; }
        
        public List<DynamicFieldDTO> DynamicFields { get; set; }

        public List<RemarkDTO> Remarks { get; set; }
        
        public List<TimelineStepDTO> Timeline { get; set; }

        public DocumentVerificationDTO? VerificationDetails { get; set; }
    }

    public class DocumentVerificationDTO
    {
        public int VerificationId { get; set; }
        public string VerificationStatus { get; set; }
        public string? VerificationRemarks { get; set; }
        public string? VerifiedDate { get; set; }
        public string? OriginalFileHash { get; set; }
        public string? FinalFileHash { get; set; }
        public string? VerificationUrl { get; set; }
        public string? VerificationTokenHash { get; set; }
        public int? FinalVerifiedAttachmentId { get; set; }
    }

    public class ApplicantDTO
    {
        public string FullName { get; set; }
        public string Nic { get; set; }
        public string Address { get; set; }
    }

    public class AttachmentDTO
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileSize { get; set; } 
        public DateTime UploadedDate { get; set; }
        public string FileType { get; set; }
        public string? DocumentRole { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
    }

    public class DynamicFieldDTO
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public class RemarkDTO
    {
        public int Id { get; set; }
        public string Author { get; set; }
        public string Role { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public string? Status { get; set; }
    }
    
    public class TimelineStepDTO
    {
        public int Step { get; set; }
        public string Title { get; set; }
        public DateTime? Timestamp { get; set; }
        public bool Completed { get; set; }
        public string Officer { get; set; }
        public string? Remarks { get; set; }
    }
}
