using EGN_Service_Management.DTO;
using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EGN_Service_Management.Database;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitizenRequestController : ControllerBase
    {
        private readonly ICitizenRequestService _citizenRequestService;
        private readonly EgnDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly INotificationService _notificationService;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Count, DateTime ResetTime)> RateLimits = new();

        public CitizenRequestController(ICitizenRequestService citizenRequestService, EgnDbContext context, IWebHostEnvironment env, INotificationService notificationService)
        {
            _citizenRequestService = citizenRequestService;
            _context = context;
            _env = env;
            _notificationService = notificationService;
        }

        private DateTime GetSriLankanTime()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"));
            }
            catch
            {
                return DateTime.UtcNow.AddHours(5.5);
            }
        }

        private async Task SendReleaseNotification(UnifiedRequest request, string gnNic)
        {
            try
            {
                await _notificationService.SendNotificationAsync(
                    recipientNic: request.CitizenNic,
                    recipientRole: "Citizen",
                    type: "approved",
                    title: "Request Accepted & Document Released",
                    message: $"Your request {request.RequestNumber} has been accepted and the document/Family Card has been released.",
                    relatedEntityType: "Request",
                    relatedEntityId: request.RequestNumber,
                    actionUrl: $"/citizen/service-requests/{request.RequestNumber}",
                    priority: "High",
                    createdBy: gnNic
                );

                if (request.CategoryId == 5 || request.CategoryId == 42)
                {
                    await _notificationService.SendNotificationAsync(
                        recipientNic: request.CitizenNic,
                        recipientRole: "Citizen",
                        type: "approved",
                        title: "Family Card Details Updated",
                        message: $"Your family card and related records have been updated successfully.",
                        relatedEntityType: "Request",
                        relatedEntityId: request.RequestNumber,
                        actionUrl: $"/citizen/registry",
                        priority: "High",
                        createdBy: gnNic
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Notification Error] {ex.Message}");
            }
        }

        // Helper to add status history with remark handling
        private void AddStatusHistory(int requestId, string status, string? remarks, string changedBy)
        {
            string remarkToStore = remarks;
            
            if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "REJECTED", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(remarkToStore))
                {
                    throw new ArgumentException("Remarks are required for Rejected status.");
                }
            }
            else if (string.IsNullOrWhiteSpace(remarkToStore))
            {
                if (string.Equals(status, "Under Review", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(status, "UNDER_REVIEW", StringComparison.OrdinalIgnoreCase))
                {
                    remarkToStore = "Request is under review.";
                }
                else if (string.Equals(status, "Verified", StringComparison.OrdinalIgnoreCase) || 
                         string.Equals(status, "VERIFIED", StringComparison.OrdinalIgnoreCase))
                {
                    remarkToStore = "Request has been verified.";
                }
                else if (string.Equals(status, "Accepted", StringComparison.OrdinalIgnoreCase) || 
                         string.Equals(status, "ACCEPTED", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    remarkToStore = "Request has been approved.";
                }
                else if (string.Equals(status, "Submitted", StringComparison.OrdinalIgnoreCase) || 
                         string.Equals(status, "SUBMITTED", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    remarkToStore = null;
                }
                else if (string.Equals(status, "In Progress", StringComparison.OrdinalIgnoreCase) || 
                         string.Equals(status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
                {
                    remarkToStore = "Request is in progress.";
                }
                else
                {
                    remarkToStore = $"Status updated to {status} by GN Officer";
                }
            }

            var history = new RequestStatusHistory
            {
                RequestId = requestId,
                Status = status,
                Remarks = remarkToStore,
                ChangedBy = changedBy,
                ChangedDate = GetSriLankanTime()
            };
            _context.RequestStatusHistories.Add(history);
        }

        private async Task UpdateCategory44WelfareStatusAsync(UnifiedRequest request, string gnNic)
        {
            if (request.CategoryId != 44) return;

            var reqData = await _context.RequestDataEntries
                .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);
            if (reqData == null || string.IsNullOrEmpty(reqData.JsonData)) return;

            using (var jsonDoc = System.Text.Json.JsonDocument.Parse(reqData.JsonData))
            {
                if (jsonDoc.RootElement.TryGetProperty("requestType", out var prop))
                {
                    var selectedTypes = new List<string>();
                    if (prop.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var val = prop.GetString();
                        if (!string.IsNullOrEmpty(val)) selectedTypes.Add(val);
                    }
                    else if (prop.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in prop.EnumerateArray())
                        {
                            if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var val = item.GetString();
                                if (!string.IsNullOrEmpty(val)) selectedTypes.Add(val);
                            }
                        }
                    }

                    if (selectedTypes.Count > 0)
                    {
                        var citizenNic = request.CitizenNic;
                        if (string.IsNullOrEmpty(citizenNic)) return;

                        var welfare = await _context.CitizenWelfareStatuses
                            .FirstOrDefaultAsync(w => w.CitizenNic == citizenNic);

                        bool isNew = false;
                        if (welfare == null)
                        {
                            isNew = true;
                            welfare = new CitizenWelfareStatus
                            {
                                CitizenNic = citizenNic,
                                IsAswesuma = false,
                                IsPublicAssistanceEligible = false,
                                IsElderlyEligible = false,
                                IsDisabilityEligible = false,
                                IsKidneyPatient = false,
                                IsPregnantMotherEligible = false,
                                IsHousingAssistanceEligible = false
                            };
                        }

                        foreach (var requestType in selectedTypes)
                        {
                            string typeLower = requestType.ToLowerInvariant().Trim();

                            if (typeLower == "aswesuma welfare benefit scheme" || typeLower == "aswesuma" || typeLower.Contains("aswesuma"))
                            {
                                welfare.IsAswesuma = true;
                            }
                            else if (typeLower == "public assistance programme" || typeLower == "public" || typeLower.Contains("public assistance"))
                            {
                                welfare.IsPublicAssistanceEligible = true;
                            }
                            else if (typeLower == "elderly assistance allowance" || typeLower == "elderly" || typeLower == "elderly allowance")
                            {
                                welfare.IsElderlyEligible = true;
                            }
                            else if (typeLower == "disability assistance allowance" || typeLower == "disability" || typeLower == "disability allowance")
                            {
                                welfare.IsDisabilityEligible = true;
                            }
                            else if (typeLower == "kidney disease patient allowance" || typeLower == "kidney" || typeLower == "kidney disease allowance")
                            {
                                welfare.IsKidneyPatient = true;
                            }
                            else if (typeLower == "pregnant mother nutritional assistance" || typeLower == "pregnant" || typeLower == "pregnant mother (poshana malla)" || typeLower.Contains("nutritional"))
                            {
                                welfare.IsPregnantMotherEligible = true;
                            }
                            else if (typeLower == "housing assistance programme" || typeLower == "housing" || typeLower == "housing assistance programmes")
                            {
                                welfare.IsHousingAssistanceEligible = true;
                            }
                        }

                        welfare.LastVerifiedDate = GetSriLankanTime();
                        welfare.VerifiedByGnOfficer = gnNic;
                        welfare.IsActive = true;

                        if (isNew)
                        {
                            _context.CitizenWelfareStatuses.Add(welfare);
                        }
                        else
                        {
                            _context.CitizenWelfareStatuses.Update(welfare);
                        }
                    }
                }
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitRequest([FromForm] CitizenRequestSubmissionDTO requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? requestDto.CitizenNic;
                var requestId = await _citizenRequestService.SubmitRequestAsync(requestDto, currentUserId);
                
                // Retrieve and Notify
                var req = await _context.UnifiedRequests.FirstOrDefaultAsync(r => r.RequestNumber == requestId);
                if (req != null)
                {
                    await _notificationService.SendNotificationAsync(
                        recipientNic: req.CitizenNic,
                        recipientRole: "Citizen",
                        type: "pending",
                        title: "Service Request Submitted",
                        message: $"Your request {req.RequestNumber} has been submitted successfully.",
                        relatedEntityType: "Request",
                        relatedEntityId: req.RequestNumber,
                        actionUrl: $"/citizen/service-requests/{req.RequestNumber}",
                        priority: "Medium",
                        createdBy: currentUserId
                    );

                    if (!string.IsNullOrEmpty(req.AssignedGnOfficerNic))
                    {
                        await _notificationService.SendNotificationAsync(
                            recipientNic: req.AssignedGnOfficerNic,
                            recipientRole: "GNOfficer",
                            type: "pending",
                            title: "New Service Request",
                            message: $"A new request {req.RequestNumber} has been submitted in your division.",
                            relatedEntityType: "Request",
                            relatedEntityId: req.RequestNumber,
                            actionUrl: $"/gn-officer/requests/{req.RequestNumber}",
                            priority: "Medium",
                            createdBy: currentUserId
                        );
                    }
                }

                return Ok(new 
                { 
                    success = true, 
                    message = "Request submitted successfully", 
                    requestId = requestId 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "An error occurred while submitting the request.", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("citizen/{nic}")]
        public async Task<IActionResult> GetCitizenRequests(string nic)
        {
            var requests = await _context.UnifiedRequests
                .Include(r => r.Service)
                .Include(r => r.Category)
                .Where(r => r.CitizenNic == nic)
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new {
                    id = r.RequestNumber,
                    citizenNIC = r.CitizenNic,
                    gnDivision = r.GnDivision,
                    serviceType = r.Service != null ? r.Service.ServiceName : "Unknown",
                    category = r.Category != null ? r.Category.CategoryName : "Unknown",
                    status = r.CurrentStatus,
                    submittedDate = r.SubmittedDate,
                    lastUpdated = r.StatusHistories.OrderByDescending(sh => sh.ChangedDate).Select(sh => (DateTime?)sh.ChangedDate).FirstOrDefault() ?? r.LastUpdated
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("gn/{gnOfficerNic}")]
        public async Task<IActionResult> GetGnOfficerRequests(string gnOfficerNic)
        {
            var requests = await _context.UnifiedRequests
                .Include(r => r.Service)
                .Include(r => r.Category)
                .Include(r => r.Citizen)
                .Where(r => r.AssignedGnOfficerNic == gnOfficerNic)
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new {
                    id = r.RequestNumber,
                    citizenNIC = r.CitizenNic,
                    citizenName = r.Citizen != null ? r.Citizen.FullName : "",
                    gnDivision = r.GnDivision,
                    serviceType = r.Service != null ? r.Service.ServiceName : "Unknown",
                    category = r.Category != null ? r.Category.CategoryName : "Unknown",
                    status = r.CurrentStatus,
                    submittedDate = r.SubmittedDate,
                    lastUpdated = r.StatusHistories.OrderByDescending(sh => sh.ChangedDate).Select(sh => (DateTime?)sh.ChangedDate).FirstOrDefault() ?? r.LastUpdated
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("details/{requestNumber}")]
        [Authorize]
        public async Task<IActionResult> GetRequestDetails(string requestNumber)
        {
            try
            {
                var request = await _context.UnifiedRequests
                    .Include(r => r.Service)
                    .Include(r => r.Category)
                    .Include(r => r.Citizen)
                        .ThenInclude(c => c.FamilyCard)
                    .Include(r => r.DynamicData)
                    .Include(r => r.Attachments)
                    .Include(r => r.AssignedOfficer)
                    .Include(r => r.StatusHistories)
                    .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);

                if (request == null)
                {
                    return NotFound(new { message = "Request not found" });
                }

                // Self-heal: Regenerate missing Family Card PDF on-the-fly
                await EnsureFamilyCardPdfExistsAsync(request);

                var dynamicFields = new System.Collections.Generic.List<EGN_Service_Management.DTO.RequestDTO.DynamicFieldDTO>();
                if (request.DynamicData != null && !string.IsNullOrEmpty(request.DynamicData.JsonData))
                {
                    try
                    {
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(request.DynamicData.JsonData);
                        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
                        {
                            string val = "";
                            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                val = prop.Value.GetString();
                            }
                            else if (prop.Value.ValueKind != System.Text.Json.JsonValueKind.Null && prop.Value.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                            {
                                val = prop.Value.ToString();
                            }

                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                // Convert camelCase to Title Case
                                var label = System.Text.RegularExpressions.Regex.Replace(prop.Name, "([a-z])([A-Z])", "$1 $2");
                                label = char.ToUpper(label[0]) + label.Substring(1);

                                dynamicFields.Add(new EGN_Service_Management.DTO.RequestDTO.DynamicFieldDTO
                                {
                                    Label = label,
                                    Value = val
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                }

                var historyEntries = request.StatusHistories.OrderBy(h => h.ChangedDate).ToList();

                // Find the first occurrence of each status in the history
                var submittedEntry = historyEntries.FirstOrDefault(h => h.Status.ToUpper() == "SUBMITTED" || h.Status.ToUpper() == "PENDING");
                var reviewEntry = historyEntries.FirstOrDefault(h => h.Status.ToUpper() == "UNDER_REVIEW" || h.Status.ToUpper() == "UNDER REVIEW" || h.Status.ToUpper() == "IN_PROGRESS" || h.Status.ToUpper() == "IN PROGRESS");
                var verifiedEntry = historyEntries.FirstOrDefault(h => h.Status.ToUpper() == "VERIFIED");
                var finalEntry = historyEntries.FirstOrDefault(h => h.Status.ToUpper() == "ACCEPTED" || h.Status.ToUpper() == "APPROVED" || h.Status.ToUpper() == "REJECTED" || h.Status.ToUpper() == "REVOKED");

                // Determine current step index (1 to 4)
                int currentStep = 1;
                var currentStatusUpper = request.CurrentStatus.ToUpper();
                if (currentStatusUpper == "UNDER_REVIEW" || currentStatusUpper == "IN PROGRESS" || currentStatusUpper == "IN_PROGRESS" || currentStatusUpper == "CHANGES_REQUESTED" || currentStatusUpper == "RESUBMITTED" || currentStatusUpper == "RETURNED")
                {
                    currentStep = 2;
                }
                else if (currentStatusUpper == "VERIFIED" || currentStatusUpper == "FORWARDEDTODESTINATIONGN")
                {
                    currentStep = 3;
                }
                else if (currentStatusUpper == "ACCEPTED" || currentStatusUpper == "APPROVED" || currentStatusUpper == "REJECTED" || currentStatusUpper == "REVOKED")
                {
                    currentStep = 4;
                }

                // Stage 4 title mapping
                string stage4Title = "Final Decision";
                if (finalEntry != null)
                {
                    stage4Title = (finalEntry.Status.ToUpper() == "REJECTED" || finalEntry.Status.ToUpper() == "REVOKED") ? "Rejected" : "Approved";
                }
                else if (currentStatusUpper == "REJECTED" || currentStatusUpper == "REVOKED")
                {
                    stage4Title = "Rejected";
                }
                else if (currentStatusUpper == "ACCEPTED" || currentStatusUpper == "APPROVED")
                {
                    stage4Title = "Approved";
                }

                // Helper local function to fetch officer name
                async Task<string> GetOfficerName(string nic)
                {
                    if (string.IsNullOrEmpty(nic)) return "";
                    var citizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == nic);
                    if (citizen != null) return citizen.FullName;

                    var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == nic);
                    if (officer != null) return officer.FullName;

                    if (nic == request.CitizenNic && request.Citizen != null)
                    {
                        return request.Citizen.FullName;
                    }

                    return nic;
                }

                var timeline = new System.Collections.Generic.List<EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO>
                {
                    new EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO 
                    { 
                        Step = 1, 
                        Title = "Submitted", 
                        Timestamp = submittedEntry?.ChangedDate ?? request.SubmittedDate, 
                        Completed = true, 
                        Officer = submittedEntry != null ? await GetOfficerName(submittedEntry.ChangedBy) : (request.Citizen?.FullName ?? ""),
                        Remarks = submittedEntry?.Remarks ?? "Request submitted"
                    },
                    new EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO 
                    { 
                        Step = 2, 
                        Title = "Under Review", 
                        Timestamp = reviewEntry?.ChangedDate, 
                        Completed = reviewEntry != null || currentStep >= 2, 
                        Officer = reviewEntry != null ? await GetOfficerName(reviewEntry.ChangedBy) : (currentStep >= 2 ? request.AssignedOfficer?.FullName ?? "" : ""),
                        Remarks = reviewEntry?.Remarks ?? (currentStep == 2 ? "Review started by GN Officer" : null)
                    },
                    new EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO 
                    { 
                        Step = 3, 
                        Title = "Verified", 
                        Timestamp = verifiedEntry?.ChangedDate ?? finalEntry?.ChangedDate, 
                        Completed = verifiedEntry != null || currentStep >= 3, 
                        Officer = verifiedEntry != null ? await GetOfficerName(verifiedEntry.ChangedBy) : (currentStep >= 3 ? request.AssignedOfficer?.FullName ?? "" : ""),
                        Remarks = verifiedEntry?.Remarks ?? (currentStep == 3 ? "Document verification completed and finalized." : null)
                    },
                    new EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO 
                    { 
                        Step = 4, 
                        Title = stage4Title, 
                        Timestamp = finalEntry?.ChangedDate, 
                        Completed = finalEntry != null || currentStep == 4, 
                        Officer = finalEntry != null ? await GetOfficerName(finalEntry.ChangedBy) : (currentStep == 4 ? request.AssignedOfficer?.FullName ?? "" : ""),
                        Remarks = finalEntry?.Remarks
                    }
                };

                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var isGnOfficer = userRole == "GNOfficer";

                var docVerification = await _context.DocumentVerifications
                    .FirstOrDefaultAsync(dv => dv.RequestId == request.RequestId);

                var isPublished = docVerification != null && docVerification.VerificationStatus == "PUBLISHED";

                var displayStatus = request.CurrentStatus ?? "Unknown";
                if (!isGnOfficer && !isPublished && (displayStatus == "Accepted" || displayStatus == "Approved" || displayStatus == "Verified"))
                {
                    displayStatus = "Verified";
                }

                // Filter final document if citizen and not published
                var sourceAttachments = request.Attachments;
                if (sourceAttachments != null)
                {
                    sourceAttachments = sourceAttachments
                        .Where(a => a.FileUrl != "/uploads/requests/dummy.pdf")
                        .ToList();
                }

                if (!isGnOfficer && !isPublished && sourceAttachments != null)
                {
                    sourceAttachments = sourceAttachments
                        .Where(a => a.DocumentRole != "FINAL_VERIFIED_DOCUMENT" && a.DocumentRole != "GENERATED_CERTIFICATE")
                        .ToList();
                }

                var remarksList = new System.Collections.Generic.List<EGN_Service_Management.DTO.RequestDTO.RemarkDTO>();
                if (request.StatusHistories != null)
                {
                    foreach (var h in request.StatusHistories.Where(h => !string.IsNullOrEmpty(h.Remarks)).OrderByDescending(h => h.ChangedDate))
                    {
                        var isCitizen = h.ChangedBy == request.CitizenNic;
                        var authorName = isCitizen ? (request.Citizen?.FullName ?? "Citizen") : (await GetOfficerName(h.ChangedBy));
                        var roleName = isCitizen ? "Citizen" : "GN Officer";
                        
                        remarksList.Add(new EGN_Service_Management.DTO.RequestDTO.RemarkDTO
                        {
                            Id = h.Id,
                            Author = authorName,
                            Role = roleName,
                            Text = h.Remarks,
                            Date = h.ChangedDate,
                            Status = h.Status
                        });
                    }
                }

                var response = new EGN_Service_Management.DTO.RequestDTO.RequestDetailsResponseDTO
                {
                    RequestId = request.RequestNumber,
                    RequestDbId = request.RequestId,
                    ServiceType = request.Service?.ServiceName ?? "Unknown",
                    Category = request.Category?.CategoryName ?? "Unknown",
                    CategoryId = request.CategoryId,
                    SubmittedDate = (request.StatusHistories?.FirstOrDefault(h => string.Equals(h.Status, "SUBMITTED", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Status, "Submitted", StringComparison.OrdinalIgnoreCase))?.ChangedDate ?? request.SubmittedDate).ToString("yyyy-MM-ddTHH:mm:ss"),
                    LastUpdated = request.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ss"),
                    CurrentStatus = displayStatus,
                    ApprovedDate = request.ApprovedDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    RejectedDate = request.RejectedDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AssignedOfficerNic = request.AssignedGnOfficerNic ?? "",
                    GnDivision = request.GnDivision ?? "Unknown",
                    DestinationGnDivision = request.DynamicData != null ? SafeGetJsonProperty(request.DynamicData.JsonData, "newGnDivision") : null,
                    AssignedOfficerName = request.AssignedOfficer?.FullName ?? "Unassigned",
                    Applicant = request.Citizen != null ? new EGN_Service_Management.DTO.RequestDTO.ApplicantDTO
                    {
                        FullName = request.Citizen.FullName ?? "Unknown",
                        Nic = request.CitizenNic ?? "Unknown",
                        Address = request.Citizen.FamilyCard?.Address ?? "Unknown Address",
                        GnDivisionId = request.Citizen.GnDivisionId
                    } : new EGN_Service_Management.DTO.RequestDTO.ApplicantDTO { FullName = "", Nic = "", Address = "", GnDivisionId = null },
                    Attachments = sourceAttachments?.Select(a => new EGN_Service_Management.DTO.RequestDTO.AttachmentDTO
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        FileUrl = a.FileUrl,
                        FileType = a.FileType,
                        UploadedDate = a.UploadedAt,
                        FileSize = a.FileSize.HasValue ? $"{(double)a.FileSize.Value / 1024 / 1024:0.##} MB" : "Unknown",
                        DocumentRole = a.DocumentRole ?? "SUPPORTING_DOCUMENT",
                        IsPrimary = a.IsPrimary,
                        IsActive = a.IsActive
                    }).ToList() ?? new System.Collections.Generic.List<EGN_Service_Management.DTO.RequestDTO.AttachmentDTO>(),
                    DynamicFields = dynamicFields,
                    Remarks = remarksList,
                    Timeline = timeline
                };

                if (docVerification != null)
                {
                    response.VerificationDetails = new DocumentVerificationDTO
                    {
                        VerificationId = docVerification.VerificationId,
                        VerificationStatus = isPublished ? "PUBLISHED" : docVerification.VerificationStatus,
                        VerificationRemarks = docVerification.VerificationRemarks,
                        VerifiedDate = docVerification.VerifiedDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                        OriginalFileHash = docVerification.OriginalFileHash,
                        FinalFileHash = isPublished || isGnOfficer ? docVerification.FinalFileHash : null,
                        VerificationUrl = isPublished || isGnOfficer ? docVerification.VerificationUrl : null,
                        VerificationTokenHash = isPublished || isGnOfficer ? docVerification.VerificationTokenHash : null,
                        FinalVerifiedAttachmentId = isPublished || isGnOfficer ? docVerification.FinalVerifiedAttachmentId : null
                    };
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace, innerException = ex.InnerException?.Message });
            }
        }

        private bool ValidateFileSignature(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            if (file.Length < 4) return false;
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            
            // Check PDF signature: %PDF (25 50 44 46)
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return true;
                
            // Check PNG signature: 89 50 4E 47
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return true;
                
            // Check JPEG signature: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return true;
                
            return false;
        }

        [HttpPost("resubmit/{requestNumber}")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> ResubmitRequest(string requestNumber, IFormFile MainDocument, List<IFormFile>? Attachments)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized(new { success = false, message = "Unauthorized access." });

            var request = await _context.UnifiedRequests
                .Include(r => r.Category)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);

            if (request == null) return NotFound(new { success = false, message = "Request not found." });
            if (request.CitizenNic != currentUserId) return Forbid();
            if (request.CurrentStatus != "CHANGES_REQUESTED") return BadRequest(new { success = false, message = "Resubmission is only allowed when changes are requested." });

            if (MainDocument == null || MainDocument.Length == 0) return BadRequest(new { success = false, message = "Exactly one main document is required." });

            // File validations
            if (MainDocument.Length > 5 * 1024 * 1024) return BadRequest(new { success = false, message = "Maximum file size allowed is 5MB." });
            var extension = Path.GetExtension(MainDocument.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(extension)) return BadRequest(new { success = false, message = "Only PDF, JPG, JPEG, and PNG files are allowed." });
            var allowedMimeTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
            if (!allowedMimeTypes.Contains(MainDocument.ContentType.ToLowerInvariant())) return BadRequest(new { success = false, message = "Invalid file type." });
            if (!ValidateFileSignature(MainDocument)) return BadRequest(new { success = false, message = "File content signature verification failed." });

            // Calculate SHA-256 Hash
            string originalFileHash = "";
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = MainDocument.OpenReadStream())
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    originalFileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }

            var verification = await _context.DocumentVerifications.FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest(new { success = false, message = "No verification record exists for this request." });

            // Mark old main documents as inactive
            var oldMainAttachments = request.Attachments.Where(a => a.DocumentRole == "MAIN_DOCUMENT" && a.IsActive);
            foreach (var oldAtt in oldMainAttachments)
            {
                oldAtt.IsActive = false;
            }

            // Save physical file
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "requests");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            var secureFileName = $"DV_{request.CitizenNic}_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(uploadFolder, secureFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await MainDocument.CopyToAsync(stream);
            }
            var fileUrl = $"/uploads/requests/{secureFileName}";

            // Add new main document attachment
            var newAttachment = new UnifiedRequestAttachment
            {
                RequestId = request.RequestId,
                FileName = MainDocument.FileName,
                FileUrl = fileUrl,
                FileType = MainDocument.ContentType,
                UploadedAt = DateTime.UtcNow,
                DocumentRole = "MAIN_DOCUMENT",
                DisplayName = "Corrected Document to be verified",
                IsPrimary = true,
                UploadedBy = currentUserId,
                FileSize = MainDocument.Length,
                MimeType = MainDocument.ContentType,
                IsActive = true
            };
            await _context.UnifiedRequestAttachments.AddAsync(newAttachment);
            await _context.SaveChangesAsync();

            // Process new supporting documents if uploaded
            if (Attachments != null && Attachments.Count > 0)
            {
                foreach (var file in Attachments)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var supportFilePath = Path.Combine(uploadFolder, uniqueFileName);
                        using (var stream = new FileStream(supportFilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        var supportUrl = $"/uploads/requests/{uniqueFileName}";
                        var supportAttachment = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = file.FileName,
                            FileUrl = supportUrl,
                            FileType = file.ContentType,
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "SUPPORTING_DOCUMENT",
                            DisplayName = file.FileName,
                            IsPrimary = false,
                            UploadedBy = currentUserId,
                            FileSize = file.Length,
                            MimeType = file.ContentType,
                            IsActive = true
                        };
                        await _context.UnifiedRequestAttachments.AddAsync(supportAttachment);
                    }
                }
            }

            // Update request details
            request.CurrentStatus = "Resubmitted";
            request.LastUpdated = GetSriLankanTime();

            // Update verification record
            verification.MainAttachmentId = newAttachment.AttachmentId;
            verification.OriginalFileHash = originalFileHash;
            verification.VerificationStatus = "RESUBMITTED";
            verification.UpdatedDate = GetSriLankanTime();

            // Add status history entry
            var history = new RequestStatusHistory
            {
                RequestId = request.RequestId,
                Status = "Resubmitted",
                Remarks = "Corrected main document uploaded by citizen",
                ChangedBy = currentUserId,
                ChangedDate = GetSriLankanTime()
            };
            await _context.RequestStatusHistories.AddAsync(history);

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Request resubmitted successfully" });
        }

        [HttpGet("gn/verifications")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> GetGnOfficerVerificationRequests()
        {
            var gnOfficerNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnOfficerNic)) return Unauthorized();

            var requests = await _context.UnifiedRequests
                .Include(r => r.Service)
                .Include(r => r.Category)
                .Include(r => r.Citizen)
                .Where(r => r.AssignedGnOfficerNic == gnOfficerNic && 
                            (r.CategoryId == 43 || r.Category.CategoryName.Contains("Document Verification") || r.Service.ServiceName.Contains("Document Verification")))
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new {
                    id = r.RequestNumber,
                    citizenNIC = r.CitizenNic,
                    citizenName = r.Citizen != null ? r.Citizen.FullName : "",
                    gnDivision = r.GnDivision,
                    serviceType = r.Service != null ? r.Service.ServiceName : "Unknown",
                    category = r.Category != null ? r.Category.CategoryName : "Unknown",
                    status = r.CurrentStatus,
                    submittedDate = r.SubmittedDate.ToString("yyyy-MM-dd"),
                    lastUpdated = r.StatusHistories.OrderByDescending(sh => sh.ChangedDate).Select(sh => (DateTime?)sh.ChangedDate).FirstOrDefault() ?? r.LastUpdated
                })
                .ToListAsync();

            return Ok(requests);
        }

        public class VerificationRemarkRequest
        {
            public string Remarks { get; set; }
        }

        [HttpPost("verification/{requestNumber}/start-review")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> StartReview(string requestNumber, [FromBody] StartReviewRequestDto? dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);

            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

            if (verification == null)
            {
                var mainAtt = await _context.UnifiedRequestAttachments
                    .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.IsActive);
                if (mainAtt == null)
                {
                    mainAtt = new UnifiedRequestAttachment
                    {
                        RequestId = request.RequestId,
                        FileName = "application_form.pdf",
                        FileUrl = "/uploads/requests/dummy.pdf",
                        FileType = "PDF",
                        UploadedAt = DateTime.UtcNow,
                        DocumentRole = "SUPPORTING_DOCUMENT",
                        DisplayName = "Application Form",
                        IsActive = true
                    };
                    _context.UnifiedRequestAttachments.Add(mainAtt);
                    await _context.SaveChangesAsync();
                }

                verification = new DocumentVerification
                {
                    RequestId = request.RequestId,
                    MainAttachmentId = mainAtt.AttachmentId,
                    AssignedGNOfficerNIC = gnNic,
                    VerificationStatus = "UNDER_REVIEW",
                    VerificationRemarks = dto?.Remarks ?? "Review started",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };
                _context.DocumentVerifications.Add(verification);
            }
            else
            {
                verification.VerificationStatus = "UNDER_REVIEW";
                verification.VerificationRemarks = dto?.Remarks ?? verification.VerificationRemarks;
                verification.UpdatedDate = GetSriLankanTime();
            }

            request.CurrentStatus = "Under Review";
            request.LastUpdated = GetSriLankanTime();

            var latestHistory = await _context.RequestStatusHistories
                .Where(h => h.RequestId == request.RequestId)
                .OrderByDescending(h => h.ChangedDate)
                .FirstOrDefaultAsync();

            if (latestHistory == null || latestHistory.Status.ToUpper() != "UNDER_REVIEW")
            {
                // Record status history for Under Review
                AddStatusHistory(request.RequestId, "Under Review", dto?.Remarks, gnNic);
            }

            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                recipientNic: request.CitizenNic,
                recipientRole: "Citizen",
                type: "pending",
                title: "Review Started",
                message: $"GN Officer has started reviewing your request {request.RequestNumber}.",
                relatedEntityType: "Request",
                relatedEntityId: request.RequestNumber,
                actionUrl: $"/citizen/service-requests/{request.RequestNumber}",
                priority: "Medium",
                createdBy: gnNic
            );

            return Ok(new { success = true, message = "Review started successfully" });
        }

        [HttpPost("verification/{requestNumber}/request-changes")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> RequestChanges(string requestNumber, [FromBody] VerificationRemarkRequest remarkReq)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(remarkReq.Remarks))
            {
                return BadRequest(new { message = "Remarks are required to request changes." });
            }

            var request = await _context.UnifiedRequests
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);

            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");

            request.CurrentStatus = "Changes Requested";
            request.LastUpdated = GetSriLankanTime();
            verification.VerificationStatus = "CHANGES_REQUESTED";
            verification.VerificationRemarks = remarkReq.Remarks;
            verification.UpdatedDate = GetSriLankanTime();

            var latestHistory = await _context.RequestStatusHistories
                .Where(h => h.RequestId == request.RequestId)
                .OrderByDescending(h => h.ChangedDate)
                .FirstOrDefaultAsync();

            if (latestHistory == null || latestHistory.Status.ToUpper() != "CHANGES_REQUESTED")
            {
                // Record status history for Changes Requested
                AddStatusHistory(request.RequestId, "Changes Requested", remarkReq.Remarks, gnNic);
            }

            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                recipientNic: request.CitizenNic,
                recipientRole: "Citizen",
                type: "rejected",
                title: "Changes Requested",
                message: $"Additional documents or changes requested for request {request.RequestNumber}.",
                relatedEntityType: "Request",
                relatedEntityId: request.RequestNumber,
                actionUrl: $"/citizen/service-requests/{request.RequestNumber}",
                priority: "High",
                createdBy: gnNic
            );

            return Ok(new { success = true, message = "Changes requested successfully" });
        }

        [HttpPost("verification/{requestNumber}/forward")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> ForwardRequest(string requestNumber, [FromBody] UpdateStatusRequestDto? dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound("Request not found.");
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            // Find destination GN Division from JSON data
            var reqData = await _context.RequestDataEntries
                .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);
            if (reqData == null || string.IsNullOrEmpty(reqData.JsonData))
            {
                return BadRequest(new { message = "Request data is missing." });
            }

            string destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "newGnDivision");
            if (string.IsNullOrEmpty(destGnDivisionId))
            {
                return BadRequest(new { message = "Destination GN division is not set in the form data." });
            }

            var destGnOfficer = await _context.GnOfficers
                .FirstOrDefaultAsync(o => o.GnDivisionId == destGnDivisionId);
            if (destGnOfficer == null)
            {
                return BadRequest(new { message = "No GN Officer is assigned to the selected destination GN Division." });
            }

            // Update AssignedGnOfficerNic and GnDivision
            request.GnDivision = destGnDivisionId;
            request.AssignedGnOfficerNic = destGnOfficer.Nic;
            request.CurrentStatus = "ForwardedToDestinationGn";
            request.LastUpdated = GetSriLankanTime();
            _context.UnifiedRequests.Update(request);

            // Update DocumentVerification status if exists
            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification != null)
            {
                verification.AssignedGNOfficerNIC = destGnOfficer.Nic;
                verification.VerificationStatus = "FORWARDED";
                verification.VerificationRemarks = dto?.Remarks ?? "Request forwarded to destination GN Officer.";
                verification.UpdatedDate = GetSriLankanTime();
                _context.DocumentVerifications.Update(verification);
            }

            AddStatusHistory(request.RequestId, "ForwardedToDestinationGn", dto?.Remarks ?? "Request forwarded to destination GN Officer.", gnNic);
            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                recipientNic: request.CitizenNic,
                recipientRole: "Citizen",
                type: "pending",
                title: "Request Forwarded",
                message: $"Your request {request.RequestNumber} has been forwarded to the destination GN Officer.",
                relatedEntityType: "Request",
                relatedEntityId: request.RequestNumber,
                actionUrl: $"/citizen/service-requests/{request.RequestNumber}",
                priority: "Medium",
                createdBy: gnNic
            );

            await _notificationService.SendNotificationAsync(
                recipientNic: destGnOfficer.Nic,
                recipientRole: "GNOfficer",
                type: "pending",
                title: "Forwarded Request Received",
                message: $"Request {request.RequestNumber} has been forwarded to you for review.",
                relatedEntityType: "Request",
                relatedEntityId: request.RequestNumber,
                actionUrl: $"/gn-officer/requests/{request.RequestNumber}",
                priority: "High",
                createdBy: gnNic
            );

            return Ok(new { success = true, message = "Request successfully forwarded to destination GN Officer." });
        }

        [HttpPost("verification/{requestNumber}/reject")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> RejectRequest(string requestNumber, [FromBody] VerificationRemarkRequest remarkReq)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(remarkReq.Remarks))
            {
                return BadRequest(new { message = "Remarks are required when rejecting a request." });
            }

            var request = await _context.UnifiedRequests
                .Include(r => r.Citizen)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);

            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

            bool isCrossGn = (request.CategoryId == 5 || request.CategoryId == 42) &&
                             request.Citizen != null &&
                             !string.IsNullOrEmpty(request.Citizen.GnDivisionId) &&
                             !string.IsNullOrEmpty(request.GnDivision) &&
                             request.Citizen.GnDivisionId != request.GnDivision;

            if (isCrossGn && request.CurrentStatus == "ForwardedToDestinationGn")
            {
                var prevGnOfficer = await _context.GnOfficers
                    .FirstOrDefaultAsync(o => o.GnDivisionId == request.Citizen.GnDivisionId);
                if (prevGnOfficer == null)
                {
                    return BadRequest(new { message = "Applicant's previous GN Officer not found." });
                }

                request.AssignedGnOfficerNic = prevGnOfficer.Nic;
                request.CurrentStatus = "Returned";
                request.LastUpdated = GetSriLankanTime();
                _context.UnifiedRequests.Update(request);

                if (verification != null)
                {
                    verification.AssignedGNOfficerNIC = prevGnOfficer.Nic;
                    verification.VerificationStatus = "RETURNED";
                    verification.VerificationRemarks = remarkReq.Remarks;
                    verification.UpdatedDate = GetSriLankanTime();
                    _context.DocumentVerifications.Update(verification);
                }

                AddStatusHistory(request.RequestId, "Returned", remarkReq.Remarks, gnNic);
                await _context.SaveChangesAsync();

                await _notificationService.SendNotificationAsync(
                    recipientNic: request.CitizenNic,
                    recipientRole: "Citizen",
                    type: "rejected",
                    title: "Request Returned",
                    message: $"Your request {request.RequestNumber} has been returned to your original GN Officer for verification.",
                    relatedEntityType: "Request",
                    relatedEntityId: request.RequestNumber,
                    actionUrl: $"/citizen/service-requests/{request.RequestNumber}",
                    priority: "High",
                    createdBy: gnNic
                );

                await _notificationService.SendNotificationAsync(
                    recipientNic: prevGnOfficer.Nic,
                    recipientRole: "GNOfficer",
                    type: "rejected",
                    title: "Forwarded Request Rejected",
                    message: $"Destination GN Officer rejected forwarded request {request.RequestNumber} and returned it to you.",
                    relatedEntityType: "Request",
                    relatedEntityId: request.RequestNumber,
                    actionUrl: $"/gn-officer/requests/{request.RequestNumber}",
                    priority: "High",
                    createdBy: gnNic
                );

                return Ok(new { success = true, message = "Request rejected and returned to the previous GN Officer." });
            }

            request.CurrentStatus = "Rejected";
            request.RejectedDate = GetSriLankanTime();
            request.LastUpdated = GetSriLankanTime();
            if (verification != null)
            {
                verification.VerificationStatus = "REJECTED";
                verification.VerificationRemarks = remarkReq.Remarks;
                verification.UpdatedDate = GetSriLankanTime();
                _context.DocumentVerifications.Update(verification);
            }

            var latestHistory = await _context.RequestStatusHistories
                .Where(h => h.RequestId == request.RequestId)
                .OrderByDescending(h => h.ChangedDate)
                .FirstOrDefaultAsync();

            if (latestHistory == null || latestHistory.Status.ToUpper() != "REJECTED")
            {
                AddStatusHistory(request.RequestId, "Rejected", remarkReq.Remarks, gnNic);
            }

            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                recipientNic: request.CitizenNic,
                recipientRole: "Citizen",
                type: "rejected",
                title: "Request Rejected",
                message: $"Your request {request.RequestNumber} has been rejected.",
                relatedEntityType: "Request",
                relatedEntityId: request.RequestNumber,
                actionUrl: $"/citizen/service-requests/{request.RequestNumber}",
                priority: "High",
                createdBy: gnNic
            );

            return Ok(new { success = true, message = "Request rejected successfully" });
        }

        [HttpGet("verification/{requestNumber}/editor")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> OpenVerificationEditor(string requestNumber)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.Category)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);

            if (request == null) return NotFound("Request not found.");
            
            if (request.Category == null || !request.Category.CategoryName.Contains("Document Verification"))
                return BadRequest("This request does not belong to the Document Verification category.");

            if (request.AssignedGnOfficerNic != gnNic) return Forbid();
            if (request.CurrentStatus?.ToUpper().Replace(" ", "_") != "UNDER_REVIEW") return BadRequest("Request must be in UNDER_REVIEW status.");

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");
            
            if (verification.VerificationStatus == "VERIFIED") return BadRequest("Verification is already finalized.");

            var mainDocs = request.Attachments.Where(a => a.DocumentRole == "MAIN_DOCUMENT" && a.IsActive).ToList();
            if (mainDocs.Count != 1) return BadRequest("Exactly one active MAIN_DOCUMENT must exist.");
            var mainDoc = mainDocs.First();

            if (mainDoc.RequestId != request.RequestId) return BadRequest("Main document does not belong to this request.");

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            var physicalPath = Path.Combine(uploadsFolder, mainDoc.FileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(physicalPath)) return BadRequest("Main document file does not exist in storage.");

            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png") return BadRequest("Unsupported file format.");

            var details = await _context.GNOfficerDigitalDetails.FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
            if (details == null)
            {
                return BadRequest("Digital Details Not Configured: Your official signature and rubber stamp have not been configured. Please configure your digital details before accepting this document.");
            }
            bool missingSig = string.IsNullOrEmpty(details.SignaturePath);
            bool missingStamp = string.IsNullOrEmpty(details.RubberStampPath);
            if (missingSig && missingStamp)
            {
                return BadRequest("Digital Details Not Configured: Your official signature and rubber stamp have not been configured. Please configure your digital details before accepting this document.");
            }
            if (missingSig)
            {
                return BadRequest("Signature Not Available: Your official digital signature has not been configured.");
            }
            if (missingStamp)
            {
                return BadRequest("Rubber Stamp Not Available: Your official rubber stamp has not been configured.");
            }

            return Ok(new { success = true, message = "Verification editor access granted", requestNumber = requestNumber });
        }

        public class SavePlacementRequest
        {
            public List<ElementPlacementDTO> Elements { get; set; } = new();
        }

        public class ElementPlacementDTO
        {
            public string ElementType { get; set; } = string.Empty;
            public int PageNumber { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Rotation { get; set; }
            public int LayerOrder { get; set; }
            public string? TextValue { get; set; }
        }

        public class RevocationRequest
        {
            public string RevocationReason { get; set; } = string.Empty;
        }

        [HttpGet("verification/{requestNumber}/placement")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> GetPlacement(string requestNumber)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");

            var elements = await _context.DocumentPlacementElements
                .Where(e => e.VerificationId == verification.VerificationId)
                .OrderBy(e => e.LayerOrder)
                .Select(e => new {
                    e.ElementId,
                    e.ElementType,
                    e.PageNumber,
                    e.X,
                    e.Y,
                    e.Width,
                    e.Height,
                    e.Rotation,
                    e.LayerOrder,
                    e.TextValue
                })
                .ToListAsync();

            return Ok(new { success = true, elements = elements });
        }

        [HttpPost("verification/{requestNumber}/placement")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> SavePlacement(string requestNumber, [FromBody] SavePlacementRequest dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");

            // Clear existing placements
            var existing = await _context.DocumentPlacementElements
                .Where(e => e.VerificationId == verification.VerificationId)
                .ToListAsync();
            _context.DocumentPlacementElements.RemoveRange(existing);

            // Add new elements
            foreach (var elem in dto.Elements)
            {
                var newElem = new DocumentPlacementElement
                {
                    VerificationId = verification.VerificationId,
                    ElementType = elem.ElementType,
                    PageNumber = elem.PageNumber,
                    X = elem.X,
                    Y = elem.Y,
                    Width = elem.Width,
                    Height = elem.Height,
                    Rotation = elem.Rotation,
                    LayerOrder = elem.LayerOrder,
                    TextValue = elem.TextValue,
                    CreatedDate = DateTime.UtcNow
                };
                _context.DocumentPlacementElements.Add(newElem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Placement elements saved successfully." });
        }

        [HttpPost("verification/{requestNumber}/preview")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> GeneratePreview(string requestNumber, [FromBody] SavePlacementRequest dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.AssignedOfficer)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");

            var originalAttachment = await _context.UnifiedRequestAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == verification.MainAttachmentId);
            if (originalAttachment == null) return BadRequest("Original document attachment not found.");

            // Find secure paths of digital details of currently authenticated officer
            var digitalDetails = await _context.GNOfficerDigitalDetails
                .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);

            var sigPath = GetPhysicalPath(digitalDetails?.SignaturePath, "gn-signatures");
            var stampPath = GetPhysicalPath(digitalDetails?.RubberStampPath, "gn-stamps");

            var originalPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), originalAttachment.FileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(originalPhysicalPath)) return BadRequest("Original file does not exist on disk.");

            // If it's an image, convert to PDF first
            var extension = Path.GetExtension(originalPhysicalPath).ToLowerInvariant();
            var pdfPath = originalPhysicalPath;
            var isImage = extension == ".png" || extension == ".jpg" || extension == ".jpeg";
            var tempPdfPath = "";

            if (isImage)
            {
                tempPdfPath = ConvertImageToPdf(originalPhysicalPath);
                pdfPath = tempPdfPath;
            }

            var previewFileName = $"preview_{requestNumber}_{DateTime.UtcNow.Ticks}.pdf";
            var previewFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
            if (!Directory.Exists(previewFolder)) Directory.CreateDirectory(previewFolder);
            var previewPhysicalPath = Path.Combine(previewFolder, previewFileName);

            // Mock verification URL for preview
            var mockUrl = verification != null && !string.IsNullOrEmpty(verification.VerificationUrl)
                ? verification.VerificationUrl
                : $"http://localhost:4200/verify-document/MOCK-{request.RequestNumber}";

            // Map DTO elements to entities
            var tempElements = dto.Elements.Select(e => new DocumentPlacementElement
            {
                ElementType = e.ElementType,
                PageNumber = e.PageNumber,
                X = e.X,
                Y = e.Y,
                Width = e.Width,
                Height = e.Height,
                Rotation = e.Rotation,
                LayerOrder = e.LayerOrder,
                TextValue = e.TextValue
            }).ToList();

            try
            {
                ApplyVerificationOverlay(pdfPath, previewPhysicalPath, tempElements, sigPath, stampPath, mockUrl, request.AssignedOfficer?.FullName ?? "GN Officer");
                
                if (!string.IsNullOrEmpty(tempPdfPath) && System.IO.File.Exists(tempPdfPath))
                {
                    System.IO.File.Delete(tempPdfPath);
                }

                var relativePreviewUrl = $"/uploads/requests/{previewFileName}";
                return Ok(new { success = true, previewUrl = relativePreviewUrl });
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(tempPdfPath) && System.IO.File.Exists(tempPdfPath))
                {
                    System.IO.File.Delete(tempPdfPath);
                }
                return StatusCode(500, new { success = false, message = "Failed to generate preview PDF.", error = ex.Message });
            }
        }

        [HttpPost("verification/{requestNumber}/finalize")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> FinalizeVerification(string requestNumber, [FromBody] FinalizeRequestDto? dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.AssignedOfficer)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            // Prevent finalizing rejected request
            if (request.CurrentStatus == "REJECTED" || request.CurrentStatus == "Rejected")
            {
                return BadRequest("Cannot finalize a rejected request.");
            }

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");
            
            if (verification.AssignedGNOfficerNIC != gnNic) return Forbid();

            // Prevent finalizing published/released documents
            if (verification.VerificationStatus == "PUBLISHED")
            {
                return BadRequest("This request has already been published and released.");
            }

            var originalAttachment = await _context.UnifiedRequestAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == verification.MainAttachmentId);
            if (originalAttachment == null) return BadRequest("Original document attachment not found.");

            var originalPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), originalAttachment.FileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(originalPhysicalPath)) return BadRequest("Original file does not exist on disk.");

            // Find secure paths of digital details of currently authenticated officer
            var digitalDetails = await _context.GNOfficerDigitalDetails
                .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);

            var sigPath = GetPhysicalPath(digitalDetails?.SignaturePath, "gn-signatures");
            var stampPath = GetPhysicalPath(digitalDetails?.RubberStampPath, "gn-stamps");

            // If it's an image, convert to PDF first
            var extension = Path.GetExtension(originalPhysicalPath).ToLowerInvariant();
            var pdfPath = originalPhysicalPath;
            var isImage = extension == ".png" || extension == ".jpg" || extension == ".jpeg";
            var tempPdfPath = "";

            if (isImage)
            {
                tempPdfPath = ConvertImageToPdf(originalPhysicalPath);
                pdfPath = tempPdfPath;
            }

            // Generate unpredictable verification token
            var secureToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            var tokenHash = HashToken(secureToken);

            var verificationUrl = $"http://localhost:4200/verify-document/{secureToken}";

            // Load saved placement elements
            var elements = await _context.DocumentPlacementElements
                .Where(e => e.VerificationId == verification.VerificationId)
                .OrderBy(e => e.LayerOrder)
                .ToListAsync();

            var finalFileName = $"final_{requestNumber}_{Guid.NewGuid().ToString("N")}.pdf";
            var requestsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
            if (!Directory.Exists(requestsFolder)) Directory.CreateDirectory(requestsFolder);
            var finalPhysicalPath = Path.Combine(requestsFolder, finalFileName);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ApplyVerificationOverlay(pdfPath, finalPhysicalPath, elements, sigPath, stampPath, verificationUrl, request.AssignedOfficer?.FullName ?? "GN Officer");
                
                if (!string.IsNullOrEmpty(tempPdfPath) && System.IO.File.Exists(tempPdfPath))
                {
                    System.IO.File.Delete(tempPdfPath);
                }

                // Compute final PDF file SHA-256 hash
                var finalFileHash = CalculateFileHash(finalPhysicalPath);
                var fileSize = new FileInfo(finalPhysicalPath).Length;

                // Deduplicate check - verify if a final verified attachment already exists
                var finalAttachment = await _context.UnifiedRequestAttachments
                    .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "FINAL_VERIFIED_DOCUMENT");

                if (finalAttachment != null)
                {
                    // Delete old physical file on disk to avoid orphan assets
                    var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), finalAttachment.FileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }

                    // Update existing record
                    finalAttachment.FileName = finalFileName;
                    finalAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                    finalAttachment.UploadedAt = GetSriLankanTime();
                    finalAttachment.FileSize = fileSize;
                    finalAttachment.UploadedBy = gnNic;
                    finalAttachment.IsActive = true;
                    _context.UnifiedRequestAttachments.Update(finalAttachment);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Create new attachment record
                    finalAttachment = new UnifiedRequestAttachment
                    {
                        RequestId = request.RequestId,
                        FileName = finalFileName,
                        FileUrl = $"/uploads/requests/{finalFileName}",
                        UploadedAt = GetSriLankanTime(),
                        FileType = "PDF",
                        DocumentRole = "FINAL_VERIFIED_DOCUMENT",
                        IsPrimary = false,
                        IsActive = true,
                        UploadedBy = gnNic,
                        MimeType = "application/pdf",
                        FileSize = fileSize
                    };
                    _context.UnifiedRequestAttachments.Add(finalAttachment);
                    await _context.SaveChangesAsync(); // Save to populate AttachmentId
                }

                // Update the Document Verification record
                verification.VerificationStatus = "VERIFIED";
                verification.VerifiedDate = GetSriLankanTime();
                verification.FinalVerifiedAttachmentId = finalAttachment.AttachmentId;
                verification.VerificationTokenHash = tokenHash;
                verification.VerificationUrl = verificationUrl;
                verification.FinalFileHash = finalFileHash;
                verification.VerificationRemarks = dto?.Remarks ?? "Verification finalized.";
                verification.UpdatedDate = GetSriLankanTime();
                _context.DocumentVerifications.Update(verification);

                // Update the request status to Verified
                request.CurrentStatus = "Verified";
                request.LastUpdated = GetSriLankanTime();
                _context.UnifiedRequests.Update(request);

                // Add to history
                var latestHistory = await _context.RequestStatusHistories
                    .Where(h => h.RequestId == request.RequestId)
                    .OrderByDescending(h => h.ChangedDate)
                    .FirstOrDefaultAsync();

                if (latestHistory == null || latestHistory.Status.ToUpper() != "VERIFIED")
                {
                    AddStatusHistory(request.RequestId, "Verified", dto?.Remarks, gnNic);
                }

                await _notificationService.SendNotificationAsync(
                    recipientNic: request.CitizenNic,
                    recipientRole: "Citizen",
                    type: "pending",
                    title: "Request Verified",
                    message: $"Your request {request.RequestNumber} has been verified by the GN Officer.",
                    relatedEntityType: "Request",
                    relatedEntityId: request.RequestNumber,
                    actionUrl: $"/citizen/service-requests/{request.RequestNumber}",
                    priority: "Medium",
                    createdBy: gnNic
                );

                await _notificationService.SendNotificationAsync(
                    recipientNic: gnNic,
                    recipientRole: "GNOfficer",
                    type: "pending",
                    title: "Ready for Certificate Generation",
                    message: $"Request {request.RequestNumber} has been verified and is ready for certificate generation/release.",
                    relatedEntityType: "Request",
                    relatedEntityId: request.RequestNumber,
                    actionUrl: $"/gn-officer/requests/{request.RequestNumber}",
                    priority: "Medium",
                    createdBy: gnNic
                );

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Verification finalized and PDF generated successfully.", downloadUrl = finalAttachment.FileUrl });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                if (System.IO.File.Exists(finalPhysicalPath))
                {
                    System.IO.File.Delete(finalPhysicalPath);
                }
                if (!string.IsNullOrEmpty(tempPdfPath) && System.IO.File.Exists(tempPdfPath))
                {
                    System.IO.File.Delete(tempPdfPath);
                }
                var innerMsg = ex.InnerException?.Message ?? "";
                Console.WriteLine($"[Finalize Error] {ex.Message}. Inner: {innerMsg}");
                return StatusCode(500, new { success = false, message = "Failed to finalize verification and generate PDF.", error = ex.Message, innerError = innerMsg });
            }
        }

        [HttpPost("verification/{requestNumber}/release")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> ReleaseVerification(string requestNumber, [FromBody] ReleaseRequestDto dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.AssignedOfficer)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");

            if (verification.VerificationStatus == "PUBLISHED")
            {
                return BadRequest("This request has already been released.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update Document Verification status to PUBLISHED and save remarks
                verification.VerificationStatus = "PUBLISHED";
                verification.VerificationRemarks = dto.Remarks;
                verification.UpdatedDate = GetSriLankanTime();
                _context.DocumentVerifications.Update(verification);

                // Update request status to Accepted (final release status)
                request.CurrentStatus = "Accepted";
                request.ApprovedDate = GetSriLankanTime();
                request.LastUpdated = GetSriLankanTime();
                _context.UnifiedRequests.Update(request);

                // Add status history record
                var latestHistory = await _context.RequestStatusHistories
                    .Where(h => h.RequestId == request.RequestId)
                    .OrderByDescending(h => h.ChangedDate)
                    .FirstOrDefaultAsync();

                if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                {
                    AddStatusHistory(request.RequestId, "Accepted", dto.Remarks, gnNic);
                }

                // Update Welfare Status for Category ID 44
                if (request.CategoryId == 44)
                {
                    await UpdateCategory44WelfareStatusAsync(request, gnNic);
                }

                await SendReleaseNotification(request, gnNic);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Verification released and published successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMsg = ex.InnerException?.Message ?? "";
                Console.WriteLine($"[Release Fail] {ex.Message}. Inner: {innerMsg}");
                return StatusCode(500, new { success = false, message = "Failed to release verification.", error = ex.Message, innerError = innerMsg });
            }
        }

        [HttpPost("verification/{requestNumber}/revoke")]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> RevokeVerification(string requestNumber, [FromBody] RevocationRequest dto)
        {
            var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(callerNic)) return Unauthorized();

            if (string.IsNullOrEmpty(dto.RevocationReason))
            {
                return BadRequest("Revocation reason is required.");
            }

            var request = await _context.UnifiedRequests
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound();

            // GN Officer must be the assigned officer to revoke, DS can revoke any
            var isDs = User.IsInRole("DSOfficer");
            if (!isDs && request.AssignedGnOfficerNic != callerNic) return Forbid();

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");

            if (verification.VerificationStatus == "REVOKED")
            {
                return BadRequest("This document is already revoked.");
            }

            // Update verification record
            verification.VerificationStatus = "REVOKED";
            verification.RevocationReason = dto.RevocationReason;
            verification.RevokedDate = GetSriLankanTime();
            verification.RevokedBy = callerNic;
            verification.UpdatedDate = GetSriLankanTime();
            _context.DocumentVerifications.Update(verification);

            // Update request status to REVOKED
            request.CurrentStatus = "Revoked";
            request.LastUpdated = GetSriLankanTime();
            _context.UnifiedRequests.Update(request);

            // Update request history
            var latestHistory = await _context.RequestStatusHistories
                .Where(h => h.RequestId == request.RequestId)
                .OrderByDescending(h => h.ChangedDate)
                .FirstOrDefaultAsync();

            if (latestHistory == null || latestHistory.Status.ToUpper() != "REVOKED")
            {
                var history = new RequestStatusHistory
                {
                    RequestId = request.RequestId,
                    Status = "Revoked",
                    Remarks = $"Verification revoked. Reason: {dto.RevocationReason}",
                    ChangedBy = callerNic,
                    ChangedDate = GetSriLankanTime()
                };
                await _context.RequestStatusHistories.AddAsync(history);
            }

            // Notification omitted (not implemented)

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Document verification revoked successfully." });
        }

        [HttpGet("public/verify/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> PublicVerifyDocument(string token)
        {
            if (token == "mock-revoked-token")
            {
                return Ok(new
                {
                    status = "revoked",
                    requestNumber = "REQ-MOCK-REVOKED",
                    documentType = "Residence Certificate",
                    citizenName = "Jane Doe",
                    maskedNic = "951****67V",
                    verifiedDate = DateTime.UtcNow.AddDays(-10),
                    gnOfficerName = "Grama Niladhari Kamal",
                    gnDivision = "Colombo West",
                    revocationReason = "Citizen reported certificate lost, new copy issued.",
                    revokedDate = DateTime.UtcNow
                });
            }

            // Apply lightweight rate limiting by IP
            var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;
            var ipLimit = RateLimits.AddOrUpdate(clientIp, 
                (1, now.AddMinutes(1)), 
                (ip, val) => {
                    if (now > val.ResetTime)
                    {
                        return (1, now.AddMinutes(1));
                    }
                    return (val.Count + 1, val.ResetTime);
                });

            if (ipLimit.Count > 30) // Max 30 scans per minute per IP
            {
                return StatusCode(429, new { success = false, message = "Too many verification requests. Please try again later." });
            }

            var tokenHash = HashToken(token);

            var verification = await _context.DocumentVerifications
                .Include(v => v.Request)
                    .ThenInclude(r => r.Citizen)
                        .ThenInclude(c => c.GnDivision)
                .Include(v => v.Request)
                    .ThenInclude(r => r.Category)
                .Include(v => v.MainAttachment)
                .Include(v => v.FinalVerifiedAttachment)
                .Include(v => v.AssignedOfficer)
                .FirstOrDefaultAsync(v => v.VerificationTokenHash == tokenHash);

            if (verification == null)
            {
                return Ok(new { status = "invalid" });
            }

            var request = verification.Request;
            if (request == null)
            {
                return Ok(new { status = "invalid" });
            }

            // Get GN division name
            var divisionName = verification.Request?.Citizen?.GnDivision?.Name 
                               ?? verification.Request?.GnDivision 
                               ?? "Unknown";

            // Status checks:
            // Published/final status: document is valid.
            // Revoked status: document is revoked and show RevocationReason and RevokedDate.
            // Any unfinished status: document is not publicly available.
            string status;
            if (verification.VerificationStatus == "PUBLISHED")
            {
                status = "valid";
            }
            else if (verification.VerificationStatus == "REVOKED")
            {
                status = "revoked";
            }
            else
            {
                status = "unavailable";
            }

            // Mask citizen NIC: keep first 3 and last 3, replace middle with asterisks
            string citizenNic = request.CitizenNic ?? string.Empty;
            string maskedNic = string.Empty;
            if (!string.IsNullOrEmpty(citizenNic))
            {
                if (citizenNic.Length <= 6)
                {
                    maskedNic = new string('*', citizenNic.Length);
                }
                else
                {
                    maskedNic = $"{citizenNic.Substring(0, 3)}{new string('*', citizenNic.Length - 6)}{citizenNic.Substring(citizenNic.Length - 3)}";
                }
            }

            // GN officer name
            string officerName = verification.AssignedOfficer?.FullName 
                                 ?? request.AssignedOfficer?.FullName 
                                 ?? "Unknown Grama Niladhari";

            // Document category
            string documentType = request.Category?.CategoryName ?? "Official Certificate";

            return Ok(new
            {
                status = status,
                requestNumber = request.RequestNumber,
                documentType = documentType,
                citizenName = request.Citizen?.FullName ?? "Unknown Citizen",
                maskedNic = maskedNic,
                verifiedDate = verification.VerifiedDate,
                gnOfficerName = officerName,
                gnDivision = divisionName,
                revocationReason = status == "revoked" ? verification.RevocationReason : null,
                revokedDate = status == "revoked" ? verification.RevokedDate : null,
                downloadUrl = status == "valid" ? verification.FinalVerifiedAttachment?.FileUrl : null
            });
        }

        [HttpGet("citizen/download-final/{requestNumber}")]
        [Authorize]
        public async Task<IActionResult> CitizenDownloadFinal(string requestNumber)
        {
            var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(callerNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.Citizen)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound();

            // Self-heal: Regenerate missing Family Card PDF on-the-fly
            await EnsureFamilyCardPdfExistsAsync(request);

            // Verify request ownership or assignment
            var isOfficer = User.IsInRole("GNOfficer") || User.IsInRole("DSOfficer");
            if (!isOfficer && request.CitizenNic != callerNic)
            {
                return Forbid();
            }

            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null) return BadRequest("No verification record found.");

            var isPublished = verification.VerificationStatus == "PUBLISHED";
            if (!isOfficer && !isPublished)
            {
                return Forbid();
            }

            if (verification.VerificationStatus == "REVOKED")
            {
                return BadRequest("This document verification has been revoked.");
            }

            var finalAttachment = await _context.UnifiedRequestAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == verification.FinalVerifiedAttachmentId);
            if (finalAttachment == null) return NotFound("Final verified document file not found.");

            var filePhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), finalAttachment.FileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(filePhysicalPath)) return NotFound("Physical file does not exist on disk.");

            var bytes = await System.IO.File.ReadAllBytesAsync(filePhysicalPath);
            return File(bytes, "application/pdf", finalAttachment.FileName);
        }

        private async Task EnsureFamilyCardPdfExistsAsync(UnifiedRequest request)
        {
            if ((request.CategoryId < 1 || request.CategoryId > 6) && request.CategoryId != 53) return;
            if (request.CurrentStatus != "Accepted" && request.CurrentStatus != "Verified") return;

            var familyCardNumber = request.Citizen?.FamilyCardNumber;
            if (string.IsNullOrEmpty(familyCardNumber))
            {
                var dbCitizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == request.CitizenNic);
                familyCardNumber = dbCitizen?.FamilyCardNumber;
            }

            if (string.IsNullOrEmpty(familyCardNumber)) return;

            var finalFileName = $"family_card_{familyCardNumber}.pdf";
            var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
            var savedFilePath = Path.Combine(uploadFolder, finalFileName);

            if (System.IO.File.Exists(savedFilePath)) return;

            try
            {
                var familyCard = await _context.FamilyCards
                    .FirstOrDefaultAsync(fc => fc.FamilyCardNumber == familyCardNumber);
                if (familyCard == null) return;

                var template = await _context.CertificateTemplates
                    .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
                if (template == null && request.ServiceId == 1)
                {
                    template = await _context.CertificateTemplates
                        .FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
                }
                if (template == null)
                {
                    template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
                }
                if (template == null) return;

                var docVerification = await _context.DocumentVerifications
                    .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                string verificationUrl = docVerification?.VerificationUrl ?? string.Empty;
                if (string.IsNullOrEmpty(verificationUrl))
                {
                    var verificationToken = Guid.NewGuid().ToString("N");
                    verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";
                    if (docVerification != null)
                    {
                        docVerification.VerificationTokenHash = HashToken(verificationToken);
                        docVerification.VerificationUrl = verificationUrl;
                        _context.DocumentVerifications.Update(docVerification);
                        await _context.SaveChangesAsync();
                    }
                }

                var officerNic = docVerification?.AssignedGNOfficerNIC ?? request.AssignedGnOfficerNic;
                if (string.IsNullOrEmpty(officerNic)) return;

                var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == officerNic);
                var digital = await _context.GNOfficerDigitalDetails.FirstOrDefaultAsync(d => d.GNOfficerNIC == officerNic);

                var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
                var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

                if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath) ||
                    string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
                {
                    return;
                }

                var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
                var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
                var officerName = officer?.FullName ?? "GN Officer";

                var editableValues = new CertificateEditableValuesDto
                {
                    IssuedDate = (docVerification?.VerifiedDate ?? request.ApprovedDate ?? DateTime.UtcNow).ToString("MMMM dd, yyyy")
                };

                var pdfBytes = RenderCertificateToPdf(
                    template.TemplateContent,
                    divisionName,
                    officerName,
                    editableValues,
                    request.RequestNumber,
                    sigPath,
                    stampPath,
                    verificationUrl
                );

                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Self-healing failed: {ex.Message}");
            }
        }

        private string GetPhysicalPath(string? relativePath, string category)
        {
            if (string.IsNullOrEmpty(relativePath)) return string.Empty;
            
            var fileName = Path.GetFileName(relativePath);
            var securePath = Path.Combine(Directory.GetCurrentDirectory(), "SecureUploads", category, fileName);
            if (System.IO.File.Exists(securePath)) return securePath;

            var fallbackPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), relativePath.TrimStart('/'));
            return System.IO.File.Exists(fallbackPath) ? fallbackPath : string.Empty;
        }

        private string ConvertImageToPdf(string imagePath)
        {
            var tempPdfPath = Path.Combine(Path.GetDirectoryName(imagePath)!, Path.GetFileNameWithoutExtension(imagePath) + "_temp.pdf");
            using (var document = new PdfSharpCore.Pdf.PdfDocument())
            {
                var page = document.AddPage();
                using (var image = PdfSharpCore.Drawing.XImage.FromFile(imagePath))
                {
                    double pageWidth = page.Width.Point;
                    double pageHeight = page.Height.Point;
                    
                    double imgWidth = image.PixelWidth;
                    double imgHeight = image.PixelHeight;
                    
                    double ratio = Math.Min(pageWidth / imgWidth, pageHeight / imgHeight);
                    double drawWidth = imgWidth * ratio;
                    double drawHeight = imgHeight * ratio;
                    
                    double x = (pageWidth - drawWidth) / 2;
                    double y = (pageHeight - drawHeight) / 2;
                    
                    using (var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page))
                    {
                        gfx.DrawImage(image, x, y, drawWidth, drawHeight);
                    }
                }
                document.Save(tempPdfPath);
            }
            return tempPdfPath;
        }

        private void ApplyVerificationOverlay(
            string sourcePdfPath, 
            string destinationPdfPath, 
            List<DocumentPlacementElement> elements,
            string signaturePhysicalPath,
            string stampPhysicalPath,
            string verificationUrl,
            string officerName)
        {
            using (var document = PdfSharpCore.Pdf.IO.PdfReader.Open(sourcePdfPath, PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Modify))
            {
                foreach (var element in elements)
                {
                    if (element.PageNumber < 1 || element.PageNumber > document.Pages.Count)
                    {
                        continue;
                    }

                    var page = document.Pages[element.PageNumber - 1];
                    double actualPageWidth = page.Width.Point;
                    double actualPageHeight = page.Height.Point;

                    double elementX = element.X * actualPageWidth;
                    double elementY = element.Y * actualPageHeight;
                    double elementWidth = element.Width * actualPageWidth;
                    double elementHeight = element.Height * actualPageHeight;

                    using (var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page))
                    {
                        if (element.ElementType == "SIGNATURE")
                        {
                            if (System.IO.File.Exists(signaturePhysicalPath))
                            {
                                using (var img = PdfSharpCore.Drawing.XImage.FromFile(signaturePhysicalPath))
                                {
                                    gfx.DrawImage(img, elementX, elementY, elementWidth, elementHeight);
                                }
                            }
                        }
                        else if (element.ElementType == "RUBBER_STAMP")
                        {
                            if (System.IO.File.Exists(stampPhysicalPath))
                            {
                                using (var img = PdfSharpCore.Drawing.XImage.FromFile(stampPhysicalPath))
                                {
                                    gfx.DrawImage(img, elementX, elementY, elementWidth, elementHeight);
                                }
                            }
                        }
                        else if (element.ElementType == "QR_CODE")
                        {
                            using (var qrGenerator = new QRCoder.QRCodeGenerator())
                            {
                                var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
                                using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
                                {
                                    byte[] qrCodeBytes = qrCode.GetGraphic(20);
                                    using (var qrImage = PdfSharpCore.Drawing.XImage.FromStream(() => new MemoryStream(qrCodeBytes)))
                                    {
                                        gfx.DrawImage(qrImage, elementX, elementY, elementWidth, elementHeight);
                                    }
                                }
                            }
                        }
                        else
                        {
                            string text = "";
                            if (element.ElementType == "VERIFIED_DATE")
                            {
                                text = element.TextValue ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
                            }
                            else if (element.ElementType == "GN_OFFICER_NAME")
                            {
                                text = element.TextValue ?? officerName;
                            }
                            else if (element.ElementType == "VERIFICATION_TEXT")
                            {
                                text = element.TextValue ?? "Verified Document";
                            }

                            if (!string.IsNullOrEmpty(text))
                            {
                                var font = new PdfSharpCore.Drawing.XFont("Helvetica", 10, PdfSharpCore.Drawing.XFontStyle.Bold);
                                gfx.DrawString(text, font, PdfSharpCore.Drawing.XBrushes.Black, 
                                    new PdfSharpCore.Drawing.XRect(elementX, elementY, elementWidth, elementHeight), 
                                    PdfSharpCore.Drawing.XStringFormats.TopLeft);
                            }
                        }
                    }
                }
                document.Save(destinationPdfPath);
            }
        }

        private string HashToken(string token)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(token);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
        }

        private string CalculateFileHash(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return Convert.ToHexString(hash).ToLowerInvariant();
                }
            }
        }

        [HttpPost("{requestNumber}/status")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> UpdateRequestStatus(string requestNumber, [FromBody] UpdateStatusRequestDto dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound();
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            if (string.Equals(dto.Status, "Rejected", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(dto.Remarks))
            {
                return BadRequest(new { message = "Remarks are required for Rejected status." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                request.CurrentStatus = dto.Status;
                request.LastUpdated = GetSriLankanTime();
                if (dto.Status == "Approved" || dto.Status == "Accepted")
                {
                    request.ApprovedDate = GetSriLankanTime();
                }
                else if (dto.Status == "Rejected")
                {
                    request.RejectedDate = GetSriLankanTime();
                }

                if (string.Equals(dto.Status, "Verified", StringComparison.OrdinalIgnoreCase))
                {
                    var verification = await _context.DocumentVerifications
                        .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
                    if (verification != null)
                    {
                        verification.VerificationStatus = "VERIFIED";
                        verification.VerificationRemarks = dto.Remarks ?? "Request verified.";
                        verification.VerifiedDate = GetSriLankanTime();
                        verification.UpdatedDate = GetSriLankanTime();
                        _context.DocumentVerifications.Update(verification);
                    }
                }

                var latestHistory = await _context.RequestStatusHistories
                    .Where(h => h.RequestId == request.RequestId)
                    .OrderByDescending(h => h.ChangedDate)
                    .FirstOrDefaultAsync();

                if (latestHistory == null || latestHistory.Status.ToUpper() != dto.Status.ToUpper())
                {
                    AddStatusHistory(request.RequestId, dto.Status, dto.Remarks, gnNic);
                }

                if (request.CategoryId == 44 && (string.Equals(dto.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(dto.Status, "Accepted", StringComparison.OrdinalIgnoreCase)))
                {
                    await UpdateCategory44WelfareStatusAsync(request, gnNic);
                }

                if (request.CategoryId == 7 && (string.Equals(dto.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(dto.Status, "Accepted", StringComparison.OrdinalIgnoreCase)))
                {
                    var exists = await _context.VoterRegistries.AnyAsync(vr => vr.CitizenNic == request.CitizenNic);
                    if (!exists)
                    {
                        var voter = new VoterRegistry
                        {
                            CitizenNic = request.CitizenNic,
                            RegisteredDate = GetSriLankanTime(),
                            RegisteredByGnOfficer = gnNic,
                            IsExported = false
                        };
                        _context.VoterRegistries.Add(voter);
                    }
                }

                if (request.CategoryId == 52 && (string.Equals(dto.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(dto.Status, "Accepted", StringComparison.OrdinalIgnoreCase)))
                {
                    var reqData = await _context.RequestDataEntries
                        .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);
                    if (reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                    {
                        using (var jsonDoc = System.Text.Json.JsonDocument.Parse(reqData.JsonData))
                        {
                            string appType = "";
                            if (jsonDoc.RootElement.TryGetProperty("applicantType", out var appTypeProp))
                            {
                                appType = appTypeProp.GetString()?.ToLowerInvariant() ?? "";
                            }

                            string? fullName = jsonDoc.RootElement.TryGetProperty("fullName", out var fnProp) ? fnProp.GetString() : null;
                            string? dobStr = jsonDoc.RootElement.TryGetProperty("dob", out var dobProp) ? dobProp.GetString() : null;
                            string? religion = jsonDoc.RootElement.TryGetProperty("religion", out var relProp) ? relProp.GetString() : null;
                            string? occupation = jsonDoc.RootElement.TryGetProperty("occupation", out var occProp) ? occProp.GetString() : null;

                            DateOnly? dob = null;
                            if (!string.IsNullOrEmpty(dobStr))
                            {
                                if (DateOnly.TryParse(dobStr, out var parsedDob))
                                    dob = parsedDob;
                                else if (DateTime.TryParse(dobStr, out var parsedDateTime))
                                    dob = DateOnly.FromDateTime(parsedDateTime);
                            }

                            if (appType == "self" || appType == "myself")
                            {
                                var citizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == request.CitizenNic);
                                if (citizen == null)
                                {
                                    throw new Exception("Citizen not found in Citizen_Master.");
                                }

                                if (fullName != null) citizen.FullName = fullName;
                                if (dob != null) citizen.Dob = dob.Value;
                                if (religion != null) citizen.Religion = religion;
                                if (occupation != null) citizen.Occupation = occupation;

                                if (jsonDoc.RootElement.TryGetProperty("monthlyIncome", out var miProp))
                                {
                                    if (miProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                                        citizen.MonthlyIncome = miProp.GetDecimal();
                                    else if (miProp.ValueKind == System.Text.Json.JsonValueKind.String && decimal.TryParse(miProp.GetString(), out var miDec))
                                        citizen.MonthlyIncome = miDec;
                                    else if (miProp.ValueKind == System.Text.Json.JsonValueKind.Null)
                                        citizen.MonthlyIncome = null;
                                }
                                _context.CitizenMasters.Update(citizen);
                            }
                            else if (appType == "child")
                            {
                                string? childTempNic = jsonDoc.RootElement.TryGetProperty("childSelection", out var csProp) ? csProp.GetString() : null;
                                if (string.IsNullOrEmpty(childTempNic))
                                {
                                    throw new Exception("Child selection NIC is missing in request data.");
                                }

                                var child = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == childTempNic);
                                if (child == null)
                                {
                                    throw new Exception($"Child with NIC {childTempNic} not found in Citizen_Master.");
                                }

                                string? newOfficialNic = jsonDoc.RootElement.TryGetProperty("newOfficialNic", out var nonProp) ? nonProp.GetString()?.Trim() : null;
                                if (!string.IsNullOrEmpty(newOfficialNic))
                                {
                                    var exists = await _context.CitizenMasters.AnyAsync(c => c.Nic == newOfficialNic);
                                    if (exists)
                                    {
                                        throw new Exception($"The new official NIC {newOfficialNic} already exists in Citizen_Master.");
                                    }

                                    var oldNic = childTempNic;
                                    var newNic = newOfficialNic;

                                    // 1. Insert new citizen record with official NIC
                                    await _context.Database.ExecuteSqlRawAsync(
                                        @"INSERT INTO Citizen_Master (Nic, FullName, Dob, Gender, MaritalStatus, Religion, Occupation, MonthlyIncome, FamilyCardNumber, RelationshipToHead, LifeStatus, GnDivisionId, PastFamilyCardNumber)
                                          SELECT {0}, {1}, {2}, Gender, MaritalStatus, {3}, {4}, MonthlyIncome, FamilyCardNumber, RelationshipToHead, LifeStatus, GnDivisionId, PastFamilyCardNumber
                                          FROM Citizen_Master WHERE Nic = {5}",
                                        newNic, 
                                        fullName ?? child.FullName, 
                                        dob ?? child.Dob, 
                                        religion ?? child.Religion, 
                                        occupation ?? child.Occupation, 
                                        oldNic);

                                    // 2. Delete old citizen record
                                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM Citizen_Master WHERE Nic = {0}", oldNic);
                                }
                                else
                                {
                                    if (fullName != null) child.FullName = fullName;
                                    if (dob != null) child.Dob = dob.Value;
                                    if (religion != null) child.Religion = religion;
                                    if (occupation != null) child.Occupation = occupation;
                                    _context.CitizenMasters.Update(child);
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Failed to update status. Transaction rolled back.", error = ex.Message });
            }
        }

        [HttpGet("certificate/editor-data/{requestNumber}")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> GetCertificateEditorData(string requestNumber)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.Category)
                .Include(r => r.Service)
                .Include(r => r.Citizen)
                    .ThenInclude(c => c.FamilyCard)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);

            if (request == null) return NotFound("Request not found.");
            if (request.ServiceId != 4 && request.ServiceId != 1) return BadRequest("This request category does not support certificate generation.");
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            // Load active template
            var template = await _context.CertificateTemplates
                .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
            
            if (template == null && request.ServiceId == 1)
            {
                // Fallback to active Family Card template (CategoryId == 1)
                template = await _context.CertificateTemplates
                    .FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
            }

            if (template == null)
            {
                // Fallback to active template if category specific not found
                template = await _context.CertificateTemplates
                    .FirstOrDefaultAsync(t => t.IsActive);
            }

            if (template == null)
            {
                return BadRequest("No active certificate template found in the database.");
            }

             // Fetch GN Officer details
             var citizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == gnNic);
             var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
             var officerName = citizen?.FullName ?? officer?.FullName ?? "GN Officer";

            // Fetch Division Name
            var division = await _context.GnDivisions
                .FirstOrDefaultAsync(d => d.Id == request.GnDivision);
            var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");

            string dsDivisionName = "Unknown";
            string districtName = "Unknown";
            if (division != null)
            {
                var dsDiv = await _context.DsDivisions
                    .Include(d => d.District)
                    .FirstOrDefaultAsync(d => d.Id == division.DsDivisionId);
                if (dsDiv != null)
                {
                    dsDivisionName = dsDiv.Name;
                    if (dsDiv.District != null)
                    {
                        districtName = dsDiv.District.Name;
                    }
                }
            }

            // Extract Required For from RequestData JSON
            string requiredFor = "Personal Use";
            var reqData = await _context.RequestDataEntries
                .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);
            if (reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(reqData.JsonData);
                    if (doc.RootElement.TryGetProperty("requiredFor", out var prop))
                    {
                        requiredFor = prop.GetString() ?? "Personal Use";
                    }
                    else if (doc.RootElement.TryGetProperty("purpose", out var prop2))
                    {
                        requiredFor = prop2.GetString() ?? "Personal Use";
                    }
                }
                catch {}
            }

            // Get digital details
            var digital = await _context.GNOfficerDigitalDetails
                .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
            
            string? stampBase64 = null;
            if (digital != null && !string.IsNullOrEmpty(digital.RubberStampPath))
            {
                var stampPath = GetPhysicalPath(digital.RubberStampPath, "gn-stamps");
                if (System.IO.File.Exists(stampPath))
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(stampPath);
                    var ext = Path.GetExtension(stampPath).ToLowerInvariant();
                    string mime = ext == ".png" ? "image/png" : "image/jpeg";
                    stampBase64 = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                }
            }

            string? sigBase64 = null;
            if (digital != null && !string.IsNullOrEmpty(digital.SignaturePath))
            {
                var sigPath = GetPhysicalPath(digital.SignaturePath, "gn-signatures");
                if (System.IO.File.Exists(sigPath))
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(sigPath);
                    var ext = Path.GetExtension(sigPath).ToLowerInvariant();
                    string mime = ext == ".png" ? "image/png" : "image/jpeg";
                    sigBase64 = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                }
            }

            string json = reqData?.JsonData ?? string.Empty;

            var response = new CertificateEditorDataResponseDto
            {
                TemplateId = template.TemplateId,
                TemplateName = template.TemplateName,
                Language = template.Language,
                RequestNumber = request.RequestNumber,
                CitizenName = request.Citizen?.FullName ?? "Unknown",
                CitizenNic = request.CitizenNic,
                ApplicantCategory = request.Category?.CategoryName ?? "Unknown",
                Status = request.CurrentStatus,
                TemplateContent = template.TemplateContent,
                RubberStampUrl = stampBase64,
                OfficerSignatureUrl = sigBase64,
                GnDivisionName = divisionName,
                GnOfficerName = officerName,
                GnDivisionId = request.GnDivision ?? string.Empty,
                DsDivisionName = dsDivisionName,
                DistrictName = districtName,
                CategoryId = request.CategoryId,
                EditableValues = new CertificateEditableValuesDto
                {
                    CitizenFullName = request.Citizen?.FullName ?? string.Empty,
                    CitizenNic = request.CitizenNic,
                    ResidentialAddress = request.Citizen?.FamilyCard?.Address ?? string.Empty,
                    RequiredFor = requiredFor,
                    IssuedDate = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
                    CertificateName = template.TemplateName,
                    ResidenceFromDate = SafeGetJsonProperty(json, "residenceFromDate") != "" ? SafeGetJsonProperty(json, "residenceFromDate") : SafeGetJsonProperty(json, "residenceFrom"),
                    ResidencePeriod = SafeGetJsonProperty(json, "residencePeriod") != "" ? SafeGetJsonProperty(json, "residencePeriod") : (SafeGetJsonProperty(json, "residencePeriodVal") != "" ? SafeGetJsonProperty(json, "residencePeriodVal") : "Over 5 Years"),
                    TemporaryAddress = SafeGetJsonProperty(json, "temporaryAddress"),
                    TemporaryFromDate = SafeGetJsonProperty(json, "temporaryFromDate") != "" ? SafeGetJsonProperty(json, "temporaryFromDate") : SafeGetJsonProperty(json, "temporaryFrom"),
                    TemporaryToDate = SafeGetJsonProperty(json, "temporaryToDate") != "" ? SafeGetJsonProperty(json, "temporaryToDate") : SafeGetJsonProperty(json, "temporaryTo"),
                    TemporaryResidenceReason = SafeGetJsonProperty(json, "temporaryResidenceReason") != "" ? SafeGetJsonProperty(json, "temporaryResidenceReason") : SafeGetJsonProperty(json, "temporaryReason"),
                    DeceasedFullName = SafeGetJsonProperty(json, "deceasedFullName") != "" ? SafeGetJsonProperty(json, "deceasedFullName") : SafeGetJsonProperty(json, "deceasedName"),
                    DeceasedNic = SafeGetJsonProperty(json, "deceasedNic"),
                    DeceasedAddress = SafeGetJsonProperty(json, "deceasedAddress"),
                    DateOfDeath = SafeGetJsonProperty(json, "dateOfDeath") != "" ? SafeGetJsonProperty(json, "dateOfDeath") : SafeGetJsonProperty(json, "deathDate"),
                    PlaceOfDeath = SafeGetJsonProperty(json, "placeOfDeath") != "" ? SafeGetJsonProperty(json, "placeOfDeath") : SafeGetJsonProperty(json, "deathPlace"),
                    ReasonOfDeath = SafeGetJsonProperty(json, "reasonOfDeath") != "" ? SafeGetJsonProperty(json, "reasonOfDeath") : SafeGetJsonProperty(json, "deathReason"),
                    Occupation = !string.IsNullOrEmpty(request.Citizen?.Occupation) ? request.Citizen.Occupation : (SafeGetJsonProperty(json, "occupation") != "" ? SafeGetJsonProperty(json, "occupation") : SafeGetJsonProperty(json, "citizenOccupation")),
                    MonthlyIncome = SafeGetJsonProperty(json, "monthlyIncome"),
                    AnnualIncome = SafeGetJsonProperty(json, "annualIncome"),
                    CertificateBody = SafeGetJsonProperty(json, "certificateBody") != "" ? SafeGetJsonProperty(json, "certificateBody") : SafeGetJsonProperty(json, "bodyText")
                }

            };


            if (request.ServiceId == 1)
            {
                if (request.CategoryId == 5 && 
                    request.CurrentStatus != "Accepted" && 
                    request.CurrentStatus != "ACCEPTED")
                {
                    string requestType = SafeGetJsonProperty(json, "requestType");
                    if (string.IsNullOrEmpty(requestType))
                    {
                        requestType = "Marriage";
                    }

                    if (string.Equals(requestType, "Marriage", StringComparison.OrdinalIgnoreCase))
                    {
                        var applicant = request.Citizen;
                        string spouseNic = SafeGetJsonProperty(json, "spouseNic");
                        var spouse = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == spouseNic);

                        if (applicant != null && spouse != null)
                        {
                            string sameGnDivisionVal = SafeGetJsonProperty(json, "sameGnDivision");
                            bool isSameGn = string.Equals(sameGnDivisionVal, "Yes", StringComparison.OrdinalIgnoreCase) || 
                                            string.Equals(sameGnDivisionVal, "Y", StringComparison.OrdinalIgnoreCase);

                            string destGnDivisionId;
                            if (isSameGn)
                            {
                                destGnDivisionId = applicant.GnDivisionId;
                            }
                            else
                            {
                                destGnDivisionId = SafeGetJsonProperty(json, "gnDivisionId");
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = SafeGetJsonProperty(json, "newGnDivision");
                                }
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = SafeGetJsonProperty(json, "gnDivision");
                                }
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = request.GnDivision ?? string.Empty;
                                }
                            }

                            // Generate unique family card number for preview
                            var digits = new string((destGnDivisionId ?? string.Empty).Where(char.IsDigit).ToArray());
                            if (string.IsNullOrEmpty(digits))
                            {
                                digits = "10";
                            }
                            
                            string newFamilyCardNumber = string.Empty;
                            int seq = 1;
                            while (true)
                            {
                                string testCardNumber = $"FC-{digits}{seq:D2}";
                                bool exists = await _context.FamilyCards.AnyAsync(fc => fc.FamilyCardNumber == testCardNumber)
                                              || string.Equals(testCardNumber, applicant.FamilyCardNumber, StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(testCardNumber, spouse.FamilyCardNumber, StringComparison.OrdinalIgnoreCase);
                                
                                if (!exists)
                                {
                                    newFamilyCardNumber = testCardNumber;
                                    break;
                                }
                                seq++;
                            }

                            // Decide Head based on gender
                            string newHeadOfHouseholdNic = SafeGetJsonProperty(json, "newHeadOfHousehold");
                            CitizenMaster headCitizen;
                            CitizenMaster nonHeadCitizen;
                            string nonHeadRelationship = "Spouse";

                            bool applicantIsMale = string.Equals(applicant.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                            bool spouseIsMale = string.Equals(spouse.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                            bool applicantIsFemale = string.Equals(applicant.Gender, "Female", StringComparison.OrdinalIgnoreCase);
                            bool spouseIsFemale = string.Equals(spouse.Gender, "Female", StringComparison.OrdinalIgnoreCase);

                            if (applicantIsFemale && spouseIsMale && string.Equals(newHeadOfHouseholdNic, applicant.Nic))
                            {
                                headCitizen = applicant;
                                nonHeadCitizen = spouse;
                                nonHeadRelationship = "Husband";
                            }
                            else if (spouseIsFemale && applicantIsMale && string.Equals(newHeadOfHouseholdNic, spouse.Nic))
                            {
                                headCitizen = spouse;
                                nonHeadCitizen = applicant;
                                nonHeadRelationship = "Husband";
                            }
                            else if (applicantIsMale)
                            {
                                headCitizen = applicant;
                                nonHeadCitizen = spouse;
                                nonHeadRelationship = spouseIsFemale ? "Wife" : "Spouse";
                            }
                            else if (spouseIsMale)
                            {
                                headCitizen = spouse;
                                nonHeadCitizen = applicant;
                                nonHeadRelationship = applicantIsFemale ? "Wife" : "Spouse";
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(newHeadOfHouseholdNic) && string.Equals(newHeadOfHouseholdNic, spouse.Nic))
                                {
                                    headCitizen = spouse;
                                    nonHeadCitizen = applicant;
                                }
                                else
                                {
                                    headCitizen = applicant;
                                    nonHeadCitizen = spouse;
                                }
                                nonHeadRelationship = "Spouse";
                            }

                            response.FamilyCardNumber = newFamilyCardNumber;
                            response.HeadOfHouseholdName = headCitizen.FullName;
                            response.HeadOfHouseholdNic = headCitizen.Nic;

                            var newAddress = SafeGetJsonProperty(json, "newAddress");
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = SafeGetJsonProperty(json, "address");
                            }
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = "No Address Provided";
                            }

                            var newHouseNumber = SafeGetJsonProperty(json, "houseNumber");
                            if (string.IsNullOrEmpty(newHouseNumber))
                            {
                                newHouseNumber = SafeGetJsonProperty(json, "newHouseNumber");
                            }

                            response.HouseNumber = newHouseNumber;
                            response.Address = newAddress;
                            response.RegisteredDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            response.FamilyCardStatus = "Active";

                            var membersList = new List<FamilyMemberRowDto>
                            {
                                new FamilyMemberRowDto
                                {
                                    Nic = headCitizen.Nic,
                                    FullName = headCitizen.FullName,
                                    Relationship = "Head",
                                    Gender = headCitizen.Gender,
                                    Dob = headCitizen.Dob.ToString("yyyy-MM-dd"),
                                    Occupation = headCitizen.Occupation ?? "Not Applicable",
                                    MonthlyIncome = headCitizen.MonthlyIncome ?? 0
                                },
                                new FamilyMemberRowDto
                                {
                                    Nic = nonHeadCitizen.Nic,
                                    FullName = nonHeadCitizen.FullName,
                                    Relationship = nonHeadRelationship,
                                    Gender = nonHeadCitizen.Gender,
                                    Dob = nonHeadCitizen.Dob.ToString("yyyy-MM-dd"),
                                    Occupation = nonHeadCitizen.Occupation ?? "Not Applicable",
                                    MonthlyIncome = nonHeadCitizen.MonthlyIncome ?? 0
                                }
                            };

                            var htmlRows = new System.Text.StringBuilder();
                            int index = 1;
                            foreach (var m in membersList)
                            {
                                htmlRows.Append("<tr>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{index++}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; font-family:monospace;\">{m.Nic}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px;\">{m.FullName}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Relationship}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Gender}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Dob}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px;\">{m.Occupation}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:right;\">Rs. {m.MonthlyIncome:N2}</td>");
                                htmlRows.Append("</tr>");
                            }

                            response.FamilyMembersRows = htmlRows.ToString();
                            response.ActiveLivingMemberCount = membersList.Count.ToString();
                            response.TotalMonthlyIncome = membersList.Sum(m => m.MonthlyIncome).ToString("N2");
                        }
                    }
                    else if (string.Equals(requestType, "DivorceOrSeparation", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(requestType, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        var memberNicsToMove = SafeGetJsonArray(json, "familyMembersToMove");
                        var movingCitizens = await _context.CitizenMasters
                            .Where(c => memberNicsToMove.Contains(c.Nic))
                            .ToListAsync();

                        if (movingCitizens.Any())
                        {
                            var applicant = request.Citizen;
                            string destGnDivisionId = string.Empty;
                            string sameGnDivisionVal = SafeGetJsonProperty(json, "sameGnDivision");
                            bool isSameGn = string.Equals(sameGnDivisionVal, "Yes", StringComparison.OrdinalIgnoreCase) || 
                                            string.Equals(sameGnDivisionVal, "Y", StringComparison.OrdinalIgnoreCase);

                            if (isSameGn && applicant != null)
                            {
                                destGnDivisionId = applicant.GnDivisionId;
                            }
                            else
                            {
                                destGnDivisionId = SafeGetJsonProperty(json, "gnDivisionId");
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = SafeGetJsonProperty(json, "newGnDivision");
                                }
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = SafeGetJsonProperty(json, "gnDivision");
                                }
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = request.GnDivision ?? string.Empty;
                                }
                            }

                            // Generate unique family card number for preview
                            var digits = new string((destGnDivisionId ?? string.Empty).Where(char.IsDigit).ToArray());
                            if (string.IsNullOrEmpty(digits))
                            {
                                digits = "10";
                            }
                            
                            string newFamilyCardNumber = string.Empty;
                            int seq = 1;
                            while (true)
                            {
                                string testCardNumber = $"FC-{digits}{seq:D2}";
                                bool exists = await _context.FamilyCards.AnyAsync(fc => fc.FamilyCardNumber == testCardNumber)
                                              || movingCitizens.Any(c => string.Equals(testCardNumber, c.FamilyCardNumber, StringComparison.OrdinalIgnoreCase));
                                
                                if (!exists)
                                {
                                    newFamilyCardNumber = testCardNumber;
                                    break;
                                }
                                seq++;
                            }

                            string newHeadOfHouseholdNic = SafeGetJsonProperty(json, "newHeadOfHousehold");
                            var headCitizen = movingCitizens.FirstOrDefault(c => c.Nic == newHeadOfHouseholdNic) ?? movingCitizens[0];

                            response.FamilyCardNumber = newFamilyCardNumber;
                            response.HeadOfHouseholdName = headCitizen.FullName;
                            response.HeadOfHouseholdNic = headCitizen.Nic;

                            var newAddress = SafeGetJsonProperty(json, "newAddress");
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = SafeGetJsonProperty(json, "address");
                            }
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = "No Address Provided";
                            }

                            var newHouseNumber = SafeGetJsonProperty(json, "houseNumber");
                            if (string.IsNullOrEmpty(newHouseNumber))
                            {
                                newHouseNumber = SafeGetJsonProperty(json, "newHouseNumber");
                            }

                            response.HouseNumber = newHouseNumber;
                            response.Address = newAddress;
                            response.RegisteredDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            response.FamilyCardStatus = "Active";

                            var membersList = new List<FamilyMemberRowDto>();
                            foreach (var movingCit in movingCitizens)
                            {
                                string rel = "Member";
                                if (movingCit.Nic == headCitizen.Nic)
                                {
                                    rel = "Head";
                                }
                                else
                                {
                                    if (movingCit.RelationshipToHead != "Head" && !string.IsNullOrEmpty(movingCit.RelationshipToHead))
                                    {
                                        rel = movingCit.RelationshipToHead;
                                    }
                                }

                                membersList.Add(new FamilyMemberRowDto
                                {
                                    Nic = movingCit.Nic,
                                    FullName = movingCit.FullName,
                                    Relationship = rel,
                                    Gender = movingCit.Gender,
                                    Dob = movingCit.Dob.ToString("yyyy-MM-dd"),
                                    Occupation = movingCit.Occupation ?? "Not Applicable",
                                    MonthlyIncome = movingCit.MonthlyIncome ?? 0
                                });
                            }

                            // Sort members so Head is first
                            membersList = membersList.OrderBy(m => m.Relationship == "Head" ? 0 : 1).ToList();

                            var htmlRows = new System.Text.StringBuilder();
                            int index = 1;
                            foreach (var m in membersList)
                            {
                                htmlRows.Append("<tr>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{index++}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; font-family:monospace;\">{m.Nic}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px;\">{m.FullName}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Relationship}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Gender}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Dob}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px;\">{m.Occupation}</td>");
                                htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:right;\">Rs. {m.MonthlyIncome:N2}</td>");
                                htmlRows.Append("</tr>");
                            }

                            response.FamilyMembersRows = htmlRows.ToString();
                            response.ActiveLivingMemberCount = membersList.Count.ToString();
                            response.TotalMonthlyIncome = membersList.Sum(m => m.MonthlyIncome).ToString("N2");
                        }
                    }
                }
                else
                {
                    var familyCardNumber = request.Citizen?.FamilyCardNumber;
                    var familyCard = await _context.FamilyCards
                        .Include(fc => fc.HeadOfHousehold)
                        .FirstOrDefaultAsync(fc => fc.FamilyCardNumber == familyCardNumber);

                    if (familyCard != null)
                    {
                        response.FamilyCardNumber = familyCard.FamilyCardNumber;
                        response.HeadOfHouseholdName = familyCard.HeadOfHousehold?.FullName ?? "Unknown";
                        response.HeadOfHouseholdNic = familyCard.HeadOfHouseholdNic;
                        response.HouseNumber = familyCard.HouseNumber;
                        response.Address = familyCard.Address;
                        response.RegisteredDate = familyCard.RegisteredDate.ToString("yyyy-MM-dd");
                        response.FamilyCardStatus = familyCard.Status;

                        // Category 4: Change Family Head
                        if (request.CategoryId == 4 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                        {
                            string newHeadNic = SafeGetJsonProperty(reqData.JsonData, "newFamilyHead");
                            if (string.IsNullOrEmpty(newHeadNic))
                            {
                                newHeadNic = SafeGetJsonProperty(reqData.JsonData, "nic");
                            }
                            if (!string.IsNullOrEmpty(newHeadNic))
                            {
                                var newHeadCit = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == newHeadNic);
                                if (newHeadCit == null)
                                {
                                    newHeadCit = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.FamilyCardNumber == familyCardNumber && c.FullName == newHeadNic);
                                }
                                if (newHeadCit != null)
                                {
                                    response.HeadOfHouseholdName = newHeadCit.FullName;
                                    response.HeadOfHouseholdNic = newHeadCit.Nic;
                                }
                            }
                        }

                        // Category 3: Removal of Deceased Family Member (if head is deceased)
                        if (request.CategoryId == 3 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                        {
                            string newHeadNic = SafeGetJsonProperty(reqData.JsonData, "newFamilyHead");
                            if (!string.IsNullOrEmpty(newHeadNic))
                            {
                                var newHeadCit = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == newHeadNic);
                                if (newHeadCit == null)
                                {
                                    newHeadCit = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.FamilyCardNumber == familyCardNumber && c.FullName == newHeadNic);
                                }
                                if (newHeadCit != null)
                                {
                                    response.HeadOfHouseholdName = newHeadCit.FullName;
                                    response.HeadOfHouseholdNic = newHeadCit.Nic;
                                }
                            }
                        }

                        // Category 42: Register to New GN Division (simulating new address details for preview)
                        if (request.CategoryId == 42 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                        {
                            var newAddress = SafeGetJsonProperty(reqData.JsonData, "newResidentialAddress");
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = SafeGetJsonProperty(reqData.JsonData, "newAddress");
                            }
                            if (!string.IsNullOrEmpty(newAddress))
                            {
                                response.Address = newAddress;
                            }

                            var newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "newHouseNumber");
                            if (!string.IsNullOrEmpty(newHouseNumber))
                            {
                                response.HouseNumber = newHouseNumber;
                            }
                        }

                        // Category 53: Family Card Address Change (simulating new address details for preview)
                        if (request.CategoryId == 53 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                        {
                            var newAddress = SafeGetJsonProperty(reqData.JsonData, "newAddress");
                            if (!string.IsNullOrEmpty(newAddress))
                            {
                                response.Address = newAddress;
                            }

                            var newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "newHouseNumber");
                            if (!string.IsNullOrEmpty(newHouseNumber))
                            {
                                response.HouseNumber = newHouseNumber;
                            }
                        }

                        var membersList = new List<FamilyMemberRowDto>();

                        var head = await _context.CitizenMasters
                            .FirstOrDefaultAsync(c => c.Nic == response.HeadOfHouseholdNic);
                        if (head != null && head.LifeStatus == "Alive")
                        {
                            membersList.Add(new FamilyMemberRowDto
                            {
                                Nic = head.Nic,
                                FullName = head.FullName,
                                Relationship = "Head",
                                Gender = head.Gender,
                                Dob = head.Dob.ToString("yyyy-MM-dd"),
                                Occupation = head.Occupation ?? "Not Applicable",
                                MonthlyIncome = head.MonthlyIncome ?? 0
                            });
                        }

                        var otherMembers = await _context.CitizenMasters
                            .Where(c => c.FamilyCardNumber == familyCard.FamilyCardNumber && c.Nic != response.HeadOfHouseholdNic && c.LifeStatus == "Alive")
                            .ToListAsync();

                        foreach (var m in otherMembers)
                        {
                            membersList.Add(new FamilyMemberRowDto
                            {
                                Nic = m.Nic,
                                FullName = m.FullName,
                                Relationship = m.RelationshipToHead ?? "Member",
                                Gender = m.Gender,
                                Dob = m.Dob.ToString("yyyy-MM-dd"),
                                Occupation = m.Occupation ?? "Not Applicable",
                                MonthlyIncome = m.MonthlyIncome ?? 0
                            });
                        }

                        if (request.CategoryId == 1)
                        {
                            string? childFullName = null;
                            string? childGender = null;
                            string? childDob = null;
                            string? childRelationship = null;
                            if (reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                            {
                                try
                                {
                                    using var doc = System.Text.Json.JsonDocument.Parse(reqData.JsonData);
                                    if (doc.RootElement.TryGetProperty("childFullName", out var p1)) childFullName = p1.GetString();
                                    if (doc.RootElement.TryGetProperty("gender", out var p2)) childGender = p2.GetString();
                                    if (doc.RootElement.TryGetProperty("dateOfBirth", out var p3)) childDob = p3.GetString();
                                    if (doc.RootElement.TryGetProperty("relationshipOfChild", out var p4)) childRelationship = p4.GetString();
                                }
                                catch {}
                            }

                            if (!string.IsNullOrEmpty(childFullName) && !string.IsNullOrEmpty(childDob))
                            {
                                DateOnly childDobDate = DateOnly.Parse(childDob);
                                bool newbornAlreadyInDb = membersList.Any(m => m.FullName == childFullName && m.Dob == childDob);
                                if (!newbornAlreadyInDb && request.CurrentStatus != "Accepted")
                                {
                                    var prefix = $"T{childDobDate:yyyyMMdd}";
                                    var existingNics = await _context.CitizenMasters.Where(c => c.Nic.StartsWith(prefix)).Select(c => c.Nic).ToListAsync();
                                    string tempNic = "";
                                    for (int i = 901; i <= 999; i++)
                                    {
                                        var testNic = $"{prefix}{i:D3}";
                                        if (!existingNics.Contains(testNic)) { tempNic = testNic; break; }
                                    }
                                    if (string.IsNullOrEmpty(tempNic))
                                    {
                                        for (int i = 1; i <= 900; i++)
                                        {
                                            var testNic = $"{prefix}{i:D3}";
                                            if (!existingNics.Contains(testNic)) { tempNic = testNic; break; }
                                        }
                                    }

                                    membersList.Add(new FamilyMemberRowDto
                                    {
                                        Nic = tempNic,
                                        FullName = childFullName,
                                        Relationship = childRelationship ?? "Son/Daughter",
                                        Gender = childGender ?? "Unknown",
                                        Dob = childDob,
                                        Occupation = "Not Applicable",
                                        MonthlyIncome = 0
                                    });
                                }
                            }
                        }
                        else if (request.CategoryId == 2 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                        {
                            string transferredNic = SafeGetJsonProperty(reqData.JsonData, "nic");
                            string relationshipToHeadVal = SafeGetJsonProperty(reqData.JsonData, "relationshipToFamilyHead");
                            if (string.IsNullOrEmpty(relationshipToHeadVal))
                            {
                                relationshipToHeadVal = "Member";
                            }

                            if (!string.IsNullOrEmpty(transferredNic) && !membersList.Any(m => m.Nic == transferredNic) && request.CurrentStatus != "Accepted")
                            {
                                var transCit = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == transferredNic);
                                if (transCit != null && transCit.LifeStatus == "Alive")
                                {
                                    membersList.Add(new FamilyMemberRowDto
                                    {
                                        Nic = transCit.Nic,
                                        FullName = transCit.FullName,
                                        Relationship = relationshipToHeadVal ?? "Member",
                                        Gender = transCit.Gender,
                                        Dob = transCit.Dob.ToString("yyyy-MM-dd"),
                                        Occupation = transCit.Occupation ?? "Not Applicable",
                                        MonthlyIncome = transCit.MonthlyIncome ?? 0
                                    });
                                }
                            }
                        }
                        else if (request.CategoryId == 3 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                        {
                            string deceasedNic = SafeGetJsonProperty(reqData.JsonData, "nic") != "" ? SafeGetJsonProperty(reqData.JsonData, "nic") : SafeGetJsonProperty(reqData.JsonData, "deceasedNic");
                            if (string.IsNullOrEmpty(deceasedNic))
                            {
                                deceasedNic = SafeGetJsonProperty(reqData.JsonData, "affectedPerson");
                            }
                            if (!string.IsNullOrEmpty(deceasedNic))
                            {
                                membersList.RemoveAll(m => m.Nic == deceasedNic);
                            }

                            // Check if new head is assigned
                            string newHeadNic = SafeGetJsonProperty(reqData.JsonData, "newFamilyHead");
                            if (!string.IsNullOrEmpty(newHeadNic))
                            {
                                var newHeadRow = membersList.FirstOrDefault(m => m.Nic == newHeadNic);
                                if (newHeadRow != null)
                                {
                                    membersList.Remove(newHeadRow);
                                    newHeadRow.Relationship = "Head";
                                    membersList.Insert(0, newHeadRow);
                                }
                            }
                        }
                        else if (request.CategoryId == 4 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                        {
                            foreach (var m in membersList)
                            {
                                if (m.Nic != response.HeadOfHouseholdNic)
                                {
                                    if (m.Relationship == "Head" || m.Relationship == "Self")
                                    {
                                        m.Relationship = "Member";
                                    }
                                }
                            }
                        }
                        else if (request.CategoryId == 6 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                        {
                            string migratedNic = SafeGetJsonProperty(reqData.JsonData, "nic");
                            if (!string.IsNullOrEmpty(migratedNic))
                            {
                                membersList.RemoveAll(m => m.Nic == migratedNic);
                            }
                        }

                        var htmlRows = new System.Text.StringBuilder();
                        int index = 1;
                        foreach (var m in membersList)
                        {
                            htmlRows.Append("<tr>");
                            htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{index++}</td>");
                            htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; font-family:monospace;\">{m.Nic}</td>");
                            htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px;\">{m.FullName}</td>");
                            htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Relationship}</td>");
                            htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Gender}</td>");
                            htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Dob}</td>");
                            htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px;\">{m.Occupation}</td>");
                            htmlRows.Append($"<td style=\"border:1px solid #000; padding:6px 3px; text-align:right;\">Rs. {m.MonthlyIncome:N2}</td>");
                            htmlRows.Append("</tr>");
                        }

                        response.FamilyMembersRows = htmlRows.ToString();
                        response.ActiveLivingMemberCount = membersList.Count.ToString();
                        response.TotalMonthlyIncome = membersList.Sum(m => m.MonthlyIncome).ToString("N2");
                    }
                }
            }

            return Ok(response);
        }

        [HttpGet("officer/rubber-stamp/{officerNic}")]
        [Authorize]
        public async Task<IActionResult> GetRubberStamp(string officerNic)
        {
            var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(callerNic)) return Unauthorized();

            var digital = await _context.GNOfficerDigitalDetails
                .FirstOrDefaultAsync(d => d.GNOfficerNIC == officerNic);
            if (digital == null || string.IsNullOrEmpty(digital.RubberStampPath)) return NotFound("Rubber stamp not found.");

            var physicalPath = GetPhysicalPath(digital.RubberStampPath, "gn-stamps");
            if (!System.IO.File.Exists(physicalPath)) return NotFound("Physical rubber stamp file not found.");

            var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            string mimeType = ext == ".png" ? "image/png" : "image/jpeg";
            return File(bytes, mimeType);
        }

        [HttpGet("officer/signature/{officerNic}")]
        [Authorize]
        public async Task<IActionResult> GetSignature(string officerNic)
        {
            var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(callerNic)) return Unauthorized();

            var digital = await _context.GNOfficerDigitalDetails
                .FirstOrDefaultAsync(d => d.GNOfficerNIC == officerNic);
            if (digital == null || string.IsNullOrEmpty(digital.SignaturePath)) return NotFound("Signature not found.");

            var physicalPath = GetPhysicalPath(digital.SignaturePath, "gn-signatures");
            if (!System.IO.File.Exists(physicalPath)) return NotFound("Physical signature file not found.");

            var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            string mimeType = ext == ".png" ? "image/png" : "image/jpeg";
            return File(bytes, mimeType);
        }

        [HttpPost("certificate/preview-pdf/{requestNumber}")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> PreviewCertificate(string requestNumber, [FromBody] GenerateCertificateRequestDto dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound("Request not found.");
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            // Load template
            var template = await _context.CertificateTemplates
                .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
            if (template == null && request.ServiceId == 1)
            {
                template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
            }
            if (template == null)
            {
                template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
            }
            if (template == null) return BadRequest("Active template not found.");

            // Fetch division name
            var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
            var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");

            // Fetch officer details
            var citizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == gnNic);
            var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
            var officerName = citizen?.FullName ?? officer?.FullName ?? "GN Officer";

            // Load digital details
            var digital = await _context.GNOfficerDigitalDetails
                .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
            
            var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
            var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

            if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
            {
                return BadRequest(new { success = false, message = "GN Officer signature file is missing. Please configure your digital details signature before generating certificates." });
            }
            if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
            {
                return BadRequest(new { success = false, message = "GN Officer rubber stamp file is missing. Please configure your digital details rubber stamp before generating certificates." });
            }

            // Mock QR code verification URL for preview
            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            var mockUrl = verification != null && !string.IsNullOrEmpty(verification.VerificationUrl)
                ? verification.VerificationUrl
                : $"http://localhost:4200/verify-document/MOCK-{request.RequestNumber}";

            // Render PDF bytes
            try
            {
                var pdfBytes = RenderCertificateToPdf(
                    template.TemplateContent,
                    divisionName,
                    officerName,
                    dto.EditableValues,
                    request.RequestNumber,
                    sigPath,
                    stampPath,
                    mockUrl
                );

                var previewFileName = $"preview_cert_{requestNumber}_{DateTime.UtcNow.Ticks}.pdf";
                var previewFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                if (!Directory.Exists(previewFolder)) Directory.CreateDirectory(previewFolder);
                var previewPhysicalPath = Path.Combine(previewFolder, previewFileName);

                await System.IO.File.WriteAllBytesAsync(previewPhysicalPath, pdfBytes);

                var relativePreviewUrl = $"/uploads/requests/{previewFileName}";
                return Ok(new { success = true, previewUrl = relativePreviewUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate preview PDF.", error = ex.Message });
            }
        }

        [HttpPost("certificate/generate/{requestNumber}")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> GenerateCertificate(string requestNumber, [FromBody] GenerateCertificateRequestDto dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.Category)
                .Include(r => r.Citizen)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound("Request not found.");
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();
            var statusUpper = request.CurrentStatus?.ToUpper().Replace(" ", "_");
            if (statusUpper != "UNDER_REVIEW" && statusUpper != "VERIFIED") return BadRequest("Request must be in UNDER_REVIEW or VERIFIED status to generate certificate.");

            // Load template
            var template = await _context.CertificateTemplates
                .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
            if (template == null && request.ServiceId == 1)
            {
                template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
            }
            if (template == null)
            {
                template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
            }
            if (template == null) return BadRequest("Active template not found.");

            // Load digital details
            var digital = await _context.GNOfficerDigitalDetails
                .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
            var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
            var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

            if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
            {
                return BadRequest(new { success = false, message = "GN Officer signature file is missing. Please configure your digital details signature before generating certificates." });
            }
            if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
            {
                return BadRequest(new { success = false, message = "GN Officer rubber stamp file is missing. Please configure your digital details rubber stamp before generating certificates." });
            }

            // Fetch division and officer names
            var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
            var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
            var citizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == gnNic);
            var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
            var officerName = citizen?.FullName ?? officer?.FullName ?? "GN Officer";

            // Generate secure QR verification token
            var verificationToken = Guid.NewGuid().ToString("N");
            var tokenHash = HashToken(verificationToken);
            var verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";

            using var transaction = await _context.Database.BeginTransactionAsync();
            string? savedFilePath = null;
            try
            {
                // Render PDF bytes
                var pdfBytes = RenderCertificateToPdf(
                    template.TemplateContent,
                    divisionName,
                    officerName,
                    dto.EditableValues,
                    request.RequestNumber,
                    sigPath,
                    stampPath,
                    verificationUrl
                );

                // File hash
                string fileHash;
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(pdfBytes);
                    fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }

                // Save file to disk using attachment system
                string finalFileName;
                if (request.CategoryId == 5)
                {
                    string familyCardNumber = "unknown";
                    var reqData = await _context.RequestDataEntries.FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);
                    if (reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                    {
                        string requestType = SafeGetJsonProperty(reqData.JsonData, "requestType");
                        if (string.IsNullOrEmpty(requestType))
                        {
                            requestType = "Marriage";
                        }

                        if (string.Equals(requestType, "Marriage", StringComparison.OrdinalIgnoreCase))
                        {
                            var applicant = request.Citizen;
                            string spouseNic = SafeGetJsonProperty(reqData.JsonData, "spouseNic");
                            var spouse = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == spouseNic);
                            if (applicant != null && spouse != null)
                            {
                                string sameGnDivisionVal = SafeGetJsonProperty(reqData.JsonData, "sameGnDivision");
                                bool isSameGn = string.Equals(sameGnDivisionVal, "Yes", StringComparison.OrdinalIgnoreCase) || 
                                                string.Equals(sameGnDivisionVal, "Y", StringComparison.OrdinalIgnoreCase);

                                string destGnDivisionId;
                                if (isSameGn)
                                {
                                    destGnDivisionId = applicant.GnDivisionId;
                                }
                                else
                                {
                                    destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivisionId");
                                    if (string.IsNullOrEmpty(destGnDivisionId))
                                    {
                                        destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "newGnDivision");
                                    }
                                    if (string.IsNullOrEmpty(destGnDivisionId))
                                    {
                                        destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivision");
                                    }
                                    if (string.IsNullOrEmpty(destGnDivisionId))
                                    {
                                        destGnDivisionId = request.GnDivision ?? string.Empty;
                                    }
                                }

                                 var digits = new string((destGnDivisionId ?? string.Empty).Where(char.IsDigit).ToArray());
                                 if (string.IsNullOrEmpty(digits))
                                 {
                                     digits = "10";
                                 }
                                 
                                 int seq = 1;
                                 while (true)
                                 {
                                     string testCardNumber = $"FC-{digits}{seq:D2}";
                                     bool exists = await _context.FamilyCards.AnyAsync(fc => fc.FamilyCardNumber == testCardNumber)
                                                   || string.Equals(testCardNumber, applicant.FamilyCardNumber, StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(testCardNumber, spouse.FamilyCardNumber, StringComparison.OrdinalIgnoreCase);
                                     
                                     if (!exists)
                                     {
                                         familyCardNumber = testCardNumber;
                                         break;
                                     }
                                     seq++;
                                 }
                            }
                        }
                        else if (string.Equals(requestType, "DivorceOrSeparation", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(requestType, "Other", StringComparison.OrdinalIgnoreCase))
                        {
                            var memberNicsToMove = SafeGetJsonArray(reqData.JsonData, "familyMembersToMove");
                            var movingCitizens = await _context.CitizenMasters
                                .Where(c => memberNicsToMove.Contains(c.Nic))
                                .ToListAsync();

                            if (movingCitizens.Any())
                            {
                                var applicant = request.Citizen;
                                string destGnDivisionId = string.Empty;
                                string sameGnDivisionVal = SafeGetJsonProperty(reqData.JsonData, "sameGnDivision");
                                bool isSameGn = string.Equals(sameGnDivisionVal, "Yes", StringComparison.OrdinalIgnoreCase) || 
                                                string.Equals(sameGnDivisionVal, "Y", StringComparison.OrdinalIgnoreCase);

                                if (isSameGn && applicant != null)
                                {
                                    destGnDivisionId = applicant.GnDivisionId;
                                }
                                else
                                {
                                    destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivisionId");
                                    if (string.IsNullOrEmpty(destGnDivisionId))
                                    {
                                        destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "newGnDivision");
                                    }
                                    if (string.IsNullOrEmpty(destGnDivisionId))
                                    {
                                        destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivision");
                                    }
                                    if (string.IsNullOrEmpty(destGnDivisionId))
                                    {
                                        destGnDivisionId = request.GnDivision ?? string.Empty;
                                    }
                                }

                                var digits = new string((destGnDivisionId ?? string.Empty).Where(char.IsDigit).ToArray());
                                if (string.IsNullOrEmpty(digits))
                                {
                                    digits = "10";
                                }
                                
                                int seq = 1;
                                while (true)
                                {
                                    string testCardNumber = $"FC-{digits}{seq:D2}";
                                    bool exists = await _context.FamilyCards.AnyAsync(fc => fc.FamilyCardNumber == testCardNumber)
                                                  || movingCitizens.Any(c => string.Equals(testCardNumber, c.FamilyCardNumber, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (!exists)
                                    {
                                        familyCardNumber = testCardNumber;
                                        break;
                                    }
                                    seq++;
                                }
                            }
                        }
                    }
                    finalFileName = $"family_card_{familyCardNumber}.pdf";
                }
                else if ((request.CategoryId >= 1 && request.CategoryId <= 6) || request.CategoryId == 53)
                {
                    string familyCardNumber = request.Citizen?.FamilyCardNumber ?? "unknown";
                    finalFileName = $"family_card_{familyCardNumber}.pdf";
                }
                else
                {
                    finalFileName = $"cert_{requestNumber}_{Guid.NewGuid().ToString("N")}.pdf";
                }
                var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                savedFilePath = Path.Combine(uploadFolder, finalFileName);

                await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);

                // Deduplicate check - verify if a generated certificate attachment already exists
                var certificateAttachment = await _context.UnifiedRequestAttachments
                    .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "GENERATED_CERTIFICATE");

                if (certificateAttachment != null)
                {
                    // Remove old file
                    var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), certificateAttachment.FileUrl.TrimStart('/'));
                    if (oldPhysicalPath != savedFilePath && System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }

                    certificateAttachment.FileName = finalFileName;
                    certificateAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                    certificateAttachment.UploadedAt = DateTime.UtcNow;
                    certificateAttachment.FileSize = pdfBytes.Length;
                    certificateAttachment.UploadedBy = gnNic;
                    certificateAttachment.IsActive = true;
                    _context.UnifiedRequestAttachments.Update(certificateAttachment);
                }
                else
                {
                    certificateAttachment = new UnifiedRequestAttachment
                    {
                        RequestId = request.RequestId,
                        FileName = finalFileName,
                        FileUrl = $"/uploads/requests/{finalFileName}",
                        FileType = "PDF",
                        UploadedAt = DateTime.UtcNow,
                        DocumentRole = "GENERATED_CERTIFICATE",
                        DisplayName = "Generated Certificate",
                        IsPrimary = true,
                        UploadedBy = gnNic,
                        FileSize = pdfBytes.Length,
                        MimeType = "application/pdf",
                        IsActive = true
                    };
                    _context.UnifiedRequestAttachments.Add(certificateAttachment);
                }
                await _context.SaveChangesAsync(); // populated AttachmentId

                // Save verification entry
                var verification = await _context.DocumentVerifications
                    .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                // Resolve MainAttachmentId to satisfy non-null foreign key constraint
                int mainAttachmentId = 0;
                var mainAtt = await _context.UnifiedRequestAttachments.FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole != "GENERATED_CERTIFICATE");
                if (mainAtt == null)
                {
                    // create dummy supporting doc
                    mainAtt = new UnifiedRequestAttachment
                    {
                        RequestId = request.RequestId,
                        FileName = "application_form.pdf",
                        FileUrl = "/uploads/requests/dummy.pdf",
                        FileType = "PDF",
                        UploadedAt = DateTime.UtcNow,
                        DocumentRole = "SUPPORTING_DOCUMENT",
                        DisplayName = "Application Form",
                        IsActive = true
                    };
                    _context.UnifiedRequestAttachments.Add(mainAtt);
                    await _context.SaveChangesAsync();
                }
                mainAttachmentId = mainAtt.AttachmentId;

                if (verification == null)
                {
                    verification = new DocumentVerification
                    {
                        RequestId = request.RequestId,
                        MainAttachmentId = mainAttachmentId,
                        AssignedGNOfficerNIC = gnNic,
                        VerificationStatus = "GENERATED",
                        VerificationRemarks = dto.Remarks ?? $"Certificate Template: {template.TemplateName}",
                        VerifiedDate = DateTime.UtcNow, // Issued Date
                        FinalVerifiedAttachmentId = certificateAttachment.AttachmentId,
                        VerificationTokenHash = tokenHash,
                        VerificationUrl = verificationUrl,
                        OriginalFileHash = template.TemplateId.ToString(), // store template ID here as string!
                        FinalFileHash = fileHash,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    _context.DocumentVerifications.Add(verification);
                }
                else
                {
                    verification.MainAttachmentId = mainAttachmentId;
                    verification.AssignedGNOfficerNIC = gnNic;
                    verification.VerificationStatus = "GENERATED";
                    verification.VerificationRemarks = dto.Remarks ?? $"Certificate Template: {template.TemplateName}";
                    verification.VerifiedDate = GetSriLankanTime(); // Issued Date
                    verification.FinalVerifiedAttachmentId = certificateAttachment.AttachmentId;
                    verification.VerificationTokenHash = tokenHash;
                    verification.VerificationUrl = verificationUrl;
                    verification.OriginalFileHash = template.TemplateId.ToString(); // store template ID here
                    verification.FinalFileHash = fileHash;
                    verification.UpdatedDate = GetSriLankanTime();
                    _context.DocumentVerifications.Update(verification);
                }

                // Update request status to Verified
                request.CurrentStatus = "Verified";
                request.LastUpdated = GetSriLankanTime();
                _context.UnifiedRequests.Update(request);

                // Add status history entry
                var latestHistory = await _context.RequestStatusHistories
                    .Where(h => h.RequestId == request.RequestId)
                    .OrderByDescending(h => h.ChangedDate)
                    .FirstOrDefaultAsync();

                if (latestHistory == null || latestHistory.Status.ToUpper() != "VERIFIED")
                {
                    AddStatusHistory(request.RequestId, "Verified", dto.Remarks, gnNic);
                }

                await _notificationService.SendNotificationAsync(
                    recipientNic: request.CitizenNic,
                    recipientRole: "Citizen",
                    type: "approved",
                    title: "Certificate Generated",
                    message: $"Certificate has been successfully generated for request {request.RequestNumber}.",
                    relatedEntityType: "Request",
                    relatedEntityId: request.RequestNumber,
                    actionUrl: $"/citizen/service-requests/{request.RequestNumber}",
                    priority: "Medium",
                    createdBy: gnNic
                );

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Certificate generated successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                {
                    try { System.IO.File.Delete(savedFilePath); } catch {}
                }
                return StatusCode(500, new { success = false, message = "Failed to generate certificate.", error = ex.Message });
            }
        }

        [HttpPost("certificate/release/{requestNumber}")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> ReleaseCertificate(string requestNumber, [FromBody] ReleaseRequestDto dto)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var request = await _context.UnifiedRequests
                .Include(r => r.Citizen)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);
            if (request == null) return NotFound("Request not found.");
            if (request.AssignedGnOfficerNic != gnNic) return Forbid();

            GnOfficer? officer = null;

            if (request.CategoryId == 1)
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (request.CurrentStatus == "Accepted")
                    {
                        return BadRequest(new { success = false, message = "The request has already been accepted." });
                    }

                    var familyCardNumber = request.Citizen?.FamilyCardNumber;
                    if (string.IsNullOrEmpty(familyCardNumber))
                    {
                        return BadRequest(new { success = false, message = "Applicant does not belong to a family card." });
                    }

                    var familyCard = await _context.FamilyCards
                        .FirstOrDefaultAsync(fc => fc.FamilyCardNumber == familyCardNumber);

                    if (familyCard == null || familyCard.Status != "Active")
                    {
                        return BadRequest(new { success = false, message = "The selected family card is invalid or inactive." });
                    }

                    officer = await _context.GnOfficers
                        .FirstOrDefaultAsync(o => o.Nic == gnNic);

                    if (officer == null || familyCard.GnDivisionId != officer.GnDivisionId)
                    {
                        return BadRequest(new { success = false, message = "The family card does not belong to the GN Officer's assigned division." });
                    }

                    var parentActive = await _context.CitizenMasters.AnyAsync(c =>
                        c.Nic == request.CitizenNic &&
                        c.FamilyCardNumber == familyCardNumber &&
                        c.LifeStatus == "Alive" &&
                        (!_context.FamilyMemberHistories.Any(h => h.CitizenNic == c.Nic && h.FamilyCardNumber == familyCardNumber) ||
                         _context.FamilyMemberHistories.Any(h => h.CitizenNic == c.Nic && h.FamilyCardNumber == familyCardNumber && h.LeftDate == null)));

                    if (!parentActive)
                    {
                        return BadRequest(new { success = false, message = "The parent or guardian is not an active member of this family card." });
                    }

                    var reqData = await _context.RequestDataEntries
                        .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);

                    if (reqData == null || string.IsNullOrEmpty(reqData.JsonData))
                    {
                        return BadRequest(new { success = false, message = "Request data is missing." });
                    }

                    string childFullName = SafeGetJsonProperty(reqData.JsonData, "childFullName");
                    string childGender = SafeGetJsonProperty(reqData.JsonData, "gender");
                    string childDob = SafeGetJsonProperty(reqData.JsonData, "dateOfBirth");
                    string childRelationship = SafeGetJsonProperty(reqData.JsonData, "relationshipOfChild");
                    string religion = SafeGetJsonProperty(reqData.JsonData, "religion");

                    if (string.IsNullOrEmpty(childFullName) || string.IsNullOrEmpty(childDob) || string.IsNullOrEmpty(childGender))
                    {
                        return BadRequest(new { success = false, message = "Newborn details (Full Name, Date of Birth, Gender) are missing in the request." });
                    }

                    DateOnly childDobDate = DateOnly.Parse(childDob);

                    bool newbornDuplicate = await _context.CitizenMasters.AnyAsync(c =>
                        c.FamilyCardNumber == familyCardNumber &&
                        c.FullName == childFullName &&
                        c.Dob == childDobDate);

                    if (newbornDuplicate)
                    {
                        return BadRequest(new { success = false, message = "This newborn has already been added to the family card." });
                    }

                    var prefix = $"T{childDobDate:yyyyMMdd}";
                    var existingNics = await _context.CitizenMasters
                        .Where(c => c.Nic.StartsWith(prefix))
                        .Select(c => c.Nic)
                        .ToListAsync();

                    string tempNic = "";
                    for (int i = 901; i <= 999; i++)
                    {
                        var testNic = $"{prefix}{i:D3}";
                        if (!existingNics.Contains(testNic)) { tempNic = testNic; break; }
                    }
                    if (string.IsNullOrEmpty(tempNic))
                    {
                        for (int i = 1; i <= 900; i++)
                        {
                            var testNic = $"{prefix}{i:D3}";
                            if (!existingNics.Contains(testNic)) { tempNic = testNic; break; }
                        }
                    }

                    if (string.IsNullOrEmpty(tempNic))
                    {
                        return StatusCode(500, new { success = false, message = "Failed to generate a unique temporary citizen identifier." });
                    }

                    var newborn = new CitizenMaster
                    {
                        Nic = tempNic,
                        FullName = childFullName,
                        Dob = childDobDate,
                        Gender = childGender,
                        MaritalStatus = "Single",
                        Religion = !string.IsNullOrEmpty(religion) ? religion : null,
                        Occupation = "Not Applicable",
                        FamilyCardNumber = familyCardNumber,
                        RelationshipToHead = childRelationship ?? "Son/Daughter",
                        LifeStatus = "Alive",
                        GnDivisionId = familyCard.GnDivisionId,
                        PastFamilyCardNumber = null,
                        MonthlyIncome = 0.00m
                    };

                    _context.CitizenMasters.Add(newborn);

                    var historyRecord = new FamilyMemberHistory
                    {
                        CitizenNic = tempNic,
                        FamilyCardNumber = familyCardNumber,
                        JoinedDate = childDobDate.ToDateTime(TimeOnly.MinValue),
                        LeftDate = null,
                        Reason = "Added as Newborn"
                    };

                    _context.FamilyMemberHistories.Add(historyRecord);
                    await _context.SaveChangesAsync();

                    var template = await _context.CertificateTemplates
                        .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
                    }
                    if (template == null)
                    {
                        throw new Exception("Active template not found.");
                    }

                    var digital = await _context.GNOfficerDigitalDetails
                        .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
                    var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
                    var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

                    if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
                    {
                        throw new Exception("GN Officer signature file is missing.");
                    }
                    if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
                    {
                        throw new Exception("GN Officer rubber stamp file is missing.");
                    }

                    var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
                    var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
                    var officerName = officer?.FullName ?? "GN Officer";

                    var verificationToken = Guid.NewGuid().ToString("N");
                    var tokenHash = HashToken(verificationToken);
                    var verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";

                    var editableValues = new CertificateEditableValuesDto
                    {
                        IssuedDate = DateTime.UtcNow.ToString("MMMM dd, yyyy")
                    };

                    var pdfBytes = RenderCertificateToPdf(
                        template.TemplateContent,
                        divisionName,
                        officerName,
                        editableValues,
                        request.RequestNumber,
                        sigPath,
                        stampPath,
                        verificationUrl
                    );

                    string fileHash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(pdfBytes);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }

                    var finalFileName = $"family_card_{familyCardNumber}.pdf";
                    var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                    var savedFilePath = Path.Combine(uploadFolder, finalFileName);

                    await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);

                    var certificateAttachment = await _context.UnifiedRequestAttachments
                        .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "GENERATED_CERTIFICATE");

                    if (certificateAttachment != null)
                    {
                        var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), certificateAttachment.FileUrl.TrimStart('/'));
                        if (oldPhysicalPath != savedFilePath && System.IO.File.Exists(oldPhysicalPath))
                        {
                            System.IO.File.Delete(oldPhysicalPath);
                        }

                        certificateAttachment.FileName = finalFileName;
                        certificateAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                        certificateAttachment.UploadedAt = DateTime.UtcNow;
                        certificateAttachment.FileSize = pdfBytes.Length;
                        certificateAttachment.UploadedBy = gnNic;
                        _context.UnifiedRequestAttachments.Update(certificateAttachment);
                    }
                    else
                    {
                        certificateAttachment = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = finalFileName,
                            FileUrl = $"/uploads/requests/{finalFileName}",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "GENERATED_CERTIFICATE",
                            DisplayName = "Generated Certificate",
                            IsPrimary = true,
                            UploadedBy = gnNic,
                            FileSize = pdfBytes.Length,
                            MimeType = "application/pdf",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(certificateAttachment);
                    }
                    await _context.SaveChangesAsync();

                    var docVerification = await _context.DocumentVerifications
                        .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                    int mainAttachmentId = 0;
                    var mainAtt = await _context.UnifiedRequestAttachments.FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole != "GENERATED_CERTIFICATE");
                    if (mainAtt == null)
                    {
                        mainAtt = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = "application_form.pdf",
                            FileUrl = "/uploads/requests/dummy.pdf",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "SUPPORTING_DOCUMENT",
                            DisplayName = "Application Form",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(mainAtt);
                        await _context.SaveChangesAsync();
                    }
                    mainAttachmentId = mainAtt.AttachmentId;

                    if (docVerification == null)
                    {
                        docVerification = new DocumentVerification
                        {
                            RequestId = request.RequestId,
                            MainAttachmentId = mainAttachmentId,
                            AssignedGNOfficerNIC = gnNic,
                            VerificationStatus = "PUBLISHED",
                            VerificationRemarks = dto.Remarks ?? "Family Card released.",
                            VerifiedDate = DateTime.UtcNow,
                            FinalVerifiedAttachmentId = certificateAttachment.AttachmentId,
                            VerificationTokenHash = tokenHash,
                            VerificationUrl = verificationUrl,
                            OriginalFileHash = template.TemplateId.ToString(),
                            FinalFileHash = fileHash,
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow
                        };
                        _context.DocumentVerifications.Add(docVerification);
                    }
                    else
                    {
                        docVerification.MainAttachmentId = mainAttachmentId;
                        docVerification.AssignedGNOfficerNIC = gnNic;
                        docVerification.VerificationStatus = "PUBLISHED";
                        docVerification.VerificationRemarks = dto.Remarks ?? "Family Card released.";
                        docVerification.VerifiedDate = GetSriLankanTime();
                        docVerification.FinalVerifiedAttachmentId = certificateAttachment.AttachmentId;
                        docVerification.VerificationTokenHash = tokenHash;
                        docVerification.VerificationUrl = verificationUrl;
                        docVerification.OriginalFileHash = template.TemplateId.ToString();
                        docVerification.FinalFileHash = fileHash;
                        docVerification.UpdatedDate = GetSriLankanTime();
                        _context.DocumentVerifications.Update(docVerification);
                    }

                    request.CurrentStatus = "Accepted";
                    request.ApprovedDate = GetSriLankanTime();
                    request.LastUpdated = GetSriLankanTime();
                    _context.UnifiedRequests.Update(request);

                    var latestHistory = await _context.RequestStatusHistories
                        .Where(h => h.RequestId == request.RequestId)
                        .OrderByDescending(h => h.ChangedDate)
                        .FirstOrDefaultAsync();

                    if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                    {
                        AddStatusHistory(request.RequestId, "Accepted", dto.Remarks, gnNic);
                    }

                    await SendReleaseNotification(request, gnNic);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Ok(new { success = true, message = "Family Card released and newborn added successfully." });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    var detailedError = ex.InnerException != null ? $"{ex.Message} (Inner: {ex.InnerException.Message})" : ex.Message;
                    return StatusCode(500, new { success = false, message = "Failed to process newborn family card request.", error = detailedError });
                }
            }
            else if (request.CategoryId == 2)
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                string? savedFilePath = null;
                try
                {
                    // 1. Validations
                    if (request == null)
                    {
                        return BadRequest(new { success = false, message = "Request not found." });
                    }
                    if (request.CategoryId != 2)
                    {
                        return BadRequest(new { success = false, message = "Request is not a Category ID 2." });
                    }
                    if (request.AssignedGnOfficerNic != gnNic)
                    {
                        return Forbid("Cannot accept a request assigned to another officer.");
                    }
                    
                    officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
                    if (officer == null)
                    {
                        return BadRequest(new { success = false, message = "Authenticated GN Officer record not found." });
                    }

                    var reqData = await _context.RequestDataEntries
                        .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);

                    if (reqData == null || string.IsNullOrEmpty(reqData.JsonData))
                    {
                        return BadRequest(new { success = false, message = "Request data is missing." });
                    }

                    string transferredNic = SafeGetJsonProperty(reqData.JsonData, "nic");
                    string relationshipToHead = SafeGetJsonProperty(reqData.JsonData, "relationshipToFamilyHead");
                    if (string.IsNullOrEmpty(relationshipToHead))
                    {
                        relationshipToHead = "Member";
                    }
                    
                    if (string.IsNullOrEmpty(transferredNic))
                    {
                        return BadRequest(new { success = false, message = "NIC of the person being added is missing." });
                    }

                    var submitterFamilyCardNumber = request.Citizen?.FamilyCardNumber;
                    if (string.IsNullOrEmpty(submitterFamilyCardNumber))
                    {
                        return BadRequest(new { success = false, message = "Submitter does not belong to a family card." });
                    }

                    var destinationFamilyCard = await _context.FamilyCards.FirstOrDefaultAsync(fc => fc.FamilyCardNumber == submitterFamilyCardNumber);
                    if (destinationFamilyCard == null || destinationFamilyCard.Status != "Active")
                    {
                        return BadRequest(new { success = false, message = "The destination family card is invalid or inactive." });
                    }

                    if (officer.GnDivisionId != destinationFamilyCard.GnDivisionId)
                    {
                        return BadRequest(new { success = false, message = "The GN Officer does not belong to the destination GN division." });
                    }

                    if (request.CurrentStatus == "Accepted" || request.CurrentStatus == "Approved" || request.CurrentStatus == "Rejected")
                    {
                        return BadRequest(new { success = false, message = "The request has already been accepted or rejected." });
                    }

                    var submitterCitizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == request.CitizenNic);
                    if (submitterCitizen == null || submitterCitizen.FamilyCardNumber != submitterFamilyCardNumber)
                    {
                        return BadRequest(new { success = false, message = "The submitter does not belong to the destination family card." });
                    }

                    var transferredCitizen = await _context.CitizenMasters.Include(c => c.FamilyCard).FirstOrDefaultAsync(c => c.Nic == transferredNic);
                    if (transferredCitizen == null)
                    {
                        return BadRequest(new { success = false, message = "The person being added does not exist in the system." });
                    }

                    if (transferredCitizen.LifeStatus != "Alive")
                    {
                        return BadRequest(new { success = false, message = "The person being added is not alive." });
                    }

                    if (transferredCitizen.FamilyCardNumber == submitterFamilyCardNumber)
                    {
                        return BadRequest(new { success = false, message = "The person is already in the destination family card." });
                    }

                    string submittedPreviousFamilyCard = SafeGetJsonProperty(reqData.JsonData, "previousFamilyCardNumber") != "" 
                        ? SafeGetJsonProperty(reqData.JsonData, "previousFamilyCardNumber") 
                        : SafeGetJsonProperty(reqData.JsonData, "familyCardNumber");

                    if (!string.Equals((transferredCitizen.FamilyCardNumber ?? "").Trim(), (submittedPreviousFamilyCard ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new { success = false, message = "The person's current family card in the database does not match the request's previous family card value." });
                    }

                    bool requiredDocsAvailable = await _context.UnifiedRequestAttachments.AnyAsync(a => a.RequestId == request.RequestId && a.IsActive);
                    if (!requiredDocsAvailable)
                    {
                        return BadRequest(new { success = false, message = "Required supporting documents are missing." });
                    }

                    var activeRequests = await _context.UnifiedRequests
                        .Include(r => r.DynamicData)
                        .Where(r => r.CategoryId == 2 && r.RequestId != request.RequestId && (r.CurrentStatus == "Pending" || r.CurrentStatus == "Submitted" || r.CurrentStatus == "Under Review" || r.CurrentStatus == "Verified" || r.CurrentStatus == "In Progress" || r.CurrentStatus == "Changes Requested" || r.CurrentStatus == "Resubmitted"))
                        .ToListAsync();
                    
                    bool hasDuplicate = activeRequests.Any(r => 
                    {
                        if (r.DynamicData == null || string.IsNullOrEmpty(r.DynamicData.JsonData)) return false;
                        var nic = SafeGetJsonProperty(r.DynamicData.JsonData, "nic");
                        return nic == transferredNic;
                    });
                    
                    if (hasDuplicate)
                    {
                        return BadRequest(new { success = false, message = "An active or completed transfer request already exists for this citizen." });
                    }

                    if (string.IsNullOrEmpty(relationshipToHead))
                    {
                        return BadRequest(new { success = false, message = "A valid relationship to the family head must be provided." });
                    }

                    // 2. Field Preservation
                    string prevFamilyCardNumber = transferredCitizen.FamilyCardNumber;
                    string prevGnDivisionId = transferredCitizen.GnDivisionId;
                    string prevAddress = transferredCitizen.FamilyCard?.Address ?? "";
                    string prevRelationshipToHead = transferredCitizen.RelationshipToHead ?? "";

                    // 3. Update Citizen_Master
                    transferredCitizen.PastFamilyCardNumber = prevFamilyCardNumber;
                    transferredCitizen.FamilyCardNumber = submitterFamilyCardNumber;
                    transferredCitizen.GnDivisionId = destinationFamilyCard.GnDivisionId;
                    transferredCitizen.RelationshipToHead = relationshipToHead;

                    _context.CitizenMasters.Update(transferredCitizen);

                    // 4. Family Member History
                    var historyRecord = new FamilyMemberHistory
                    {
                        CitizenNic = transferredNic,
                        FamilyCardNumber = submitterFamilyCardNumber,
                        JoinedDate = GetSriLankanTime(),
                        LeftDate = null,
                        Reason = "Family member transfer via Category 2 Request",
                        
                        PreviousFamilyCard = prevFamilyCardNumber,
                        PreviousGnDivision = prevGnDivisionId,
                        DestinationGnDivision = destinationFamilyCard.GnDivisionId,
                        PreviousRelationship = prevRelationshipToHead,
                        NewRelationship = relationshipToHead,
                        RequestId = request.RequestId,
                        ActionType = "Family Card Transfer",
                        ChangedBy = gnNic,
                        ChangedDate = GetSriLankanTime(),
                        Remarks = $"Transferred from GN Division {prevGnDivisionId ?? "Unknown"} (Family Card {prevFamilyCardNumber ?? "None"}) to Destination GN Division {destinationFamilyCard.GnDivisionId} (Family Card {submitterFamilyCardNumber})."
                    };
                    _context.FamilyMemberHistories.Add(historyRecord);
                    await _context.SaveChangesAsync();

                    var destinationGnOfficerNic = await _context.GnOfficers
                        .Where(g => g.GnDivisionId == destinationFamilyCard.GnDivisionId)
                        .Select(g => g.Nic)
                        .FirstOrDefaultAsync();

                    // 5. Invoke Reusable GN Division Change Method
                    await _citizenRequestService.HandleCitizenGnDivisionChangeAsync(
                        transferredNic, 
                        prevGnDivisionId ?? string.Empty, 
                        destinationFamilyCard.GnDivisionId, 
                        prevFamilyCardNumber ?? string.Empty, 
                        destinationGnOfficerNic ?? string.Empty, 
                        gnNic,
                        request.RequestId
                    );

                    // 6. Complete Category 2 Request & Generate PDF
                    var template = await _context.CertificateTemplates
                        .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
                    if (template == null && request.ServiceId == 1)
                    {
                        template = await _context.CertificateTemplates
                            .FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
                    }
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
                    }
                    if (template == null)
                    {
                        throw new Exception("Active template not found.");
                    }

                    var digital = await _context.GNOfficerDigitalDetails
                        .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
                    var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
                    var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

                    if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
                    {
                        throw new Exception("GN Officer signature file is missing.");
                    }
                    if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
                    {
                        throw new Exception("GN Officer rubber stamp file is missing.");
                    }

                    var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
                    var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
                    var officerName = officer?.FullName ?? "GN Officer";

                    var verificationToken = Guid.NewGuid().ToString("N");
                    var tokenHash = HashToken(verificationToken);
                    var verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";

                    var editableValues = new CertificateEditableValuesDto
                    {
                        IssuedDate = DateTime.UtcNow.ToString("MMMM dd, yyyy")
                    };

                    var pdfBytes = RenderCertificateToPdf(
                        template.TemplateContent,
                        divisionName,
                        officerName,
                        editableValues,
                        request.RequestNumber,
                        sigPath,
                        stampPath,
                        verificationUrl
                    );

                    string fileHash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(pdfBytes);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }

                    var finalFileName = $"family_card_{submitterFamilyCardNumber}.pdf";
                    var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                    savedFilePath = Path.Combine(uploadFolder, finalFileName);

                    await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);

                    var certificateAttachment = await _context.UnifiedRequestAttachments
                        .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "GENERATED_CERTIFICATE");

                    if (certificateAttachment != null)
                    {
                        var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), certificateAttachment.FileUrl.TrimStart('/'));
                        if (oldPhysicalPath != savedFilePath && System.IO.File.Exists(oldPhysicalPath))
                        {
                            try { System.IO.File.Delete(oldPhysicalPath); } catch {}
                        }

                        certificateAttachment.FileName = finalFileName;
                        certificateAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                        certificateAttachment.UploadedAt = DateTime.UtcNow;
                        certificateAttachment.FileSize = pdfBytes.Length;
                        certificateAttachment.UploadedBy = gnNic;
                        _context.UnifiedRequestAttachments.Update(certificateAttachment);
                    }
                    else
                    {
                        certificateAttachment = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = finalFileName,
                            FileUrl = $"/uploads/requests/{finalFileName}",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "GENERATED_CERTIFICATE",
                            DisplayName = "Generated Certificate",
                            IsPrimary = true,
                            UploadedBy = gnNic,
                            FileSize = pdfBytes.Length,
                            MimeType = "application/pdf",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(certificateAttachment);
                    }
                    await _context.SaveChangesAsync();

                    var docVerification = await _context.DocumentVerifications
                        .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                    int mainAttachmentId = 0;
                    var mainAtt = await _context.UnifiedRequestAttachments.FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole != "GENERATED_CERTIFICATE");
                    if (mainAtt == null)
                    {
                        mainAtt = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = "application_form.pdf",
                            FileUrl = "/uploads/requests/dummy.pdf",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "SUPPORTING_DOCUMENT",
                            DisplayName = "Application Form",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(mainAtt);
                        await _context.SaveChangesAsync();
                    }
                    mainAttachmentId = mainAtt.AttachmentId;

                    if (docVerification == null)
                    {
                        docVerification = new DocumentVerification
                        {
                            RequestId = request.RequestId,
                            MainAttachmentId = mainAttachmentId,
                            AssignedGNOfficerNIC = gnNic,
                            VerificationStatus = "PUBLISHED",
                            VerificationRemarks = dto.Remarks ?? "Family Card released.",
                            VerifiedDate = DateTime.UtcNow,
                            FinalVerifiedAttachmentId = certificateAttachment.AttachmentId,
                            VerificationTokenHash = tokenHash,
                            VerificationUrl = verificationUrl,
                            OriginalFileHash = template.TemplateId.ToString(),
                            FinalFileHash = fileHash,
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow
                        };
                        _context.DocumentVerifications.Add(docVerification);
                    }
                    else
                    {
                        docVerification.MainAttachmentId = mainAttachmentId;
                        docVerification.AssignedGNOfficerNIC = gnNic;
                        docVerification.VerificationStatus = "PUBLISHED";
                        docVerification.VerificationRemarks = dto.Remarks ?? "Family Card released.";
                        docVerification.VerifiedDate = GetSriLankanTime();
                        docVerification.FinalVerifiedAttachmentId = certificateAttachment.AttachmentId;
                        docVerification.VerificationTokenHash = tokenHash;
                        docVerification.VerificationUrl = verificationUrl;
                        docVerification.OriginalFileHash = template.TemplateId.ToString();
                        docVerification.FinalFileHash = fileHash;
                        docVerification.UpdatedDate = GetSriLankanTime();
                        _context.DocumentVerifications.Update(docVerification);
                    }

                    request.CurrentStatus = "Accepted";
                    request.ApprovedDate = GetSriLankanTime();
                    request.LastUpdated = GetSriLankanTime();
                    _context.UnifiedRequests.Update(request);

                    var latestHistory = await _context.RequestStatusHistories
                        .Where(h => h.RequestId == request.RequestId)
                        .OrderByDescending(h => h.ChangedDate)
                        .FirstOrDefaultAsync();

                    if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                    {
                        AddStatusHistory(request.RequestId, "Accepted", dto.Remarks ?? "Family Member added successfully.", gnNic);
                    }

                    await SendReleaseNotification(request, gnNic);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Ok(new { success = true, message = "Family member added and transfer processed successfully." });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                    {
                        try { System.IO.File.Delete(savedFilePath); } catch {}
                    }
                    var detailedError = ex.InnerException != null ? $"{ex.Message} (Inner: {ex.InnerException.Message})" : ex.Message;
                    return StatusCode(500, new { success = false, message = "Failed to process family member transfer.", error = detailedError });
                }
            }
            else if (request.CategoryId == 3)
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                string? savedFilePath = null;
                try
                {
                    // 1. Validations
                    if (request == null)
                    {
                        return BadRequest(new { success = false, message = "Request not found." });
                    }
                    if (request.CategoryId != 3)
                    {
                        return BadRequest(new { success = false, message = "Request is not a Category ID 3." });
                    }
                    if (request.AssignedGnOfficerNic != gnNic)
                    {
                        return Forbid("Cannot accept a request assigned to another officer.");
                    }
                    
                    officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
                    if (officer == null)
                    {
                        return BadRequest(new { success = false, message = "Authenticated GN Officer record not found." });
                    }

                    var reqData = await _context.RequestDataEntries
                        .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);

                    if (reqData == null || string.IsNullOrEmpty(reqData.JsonData))
                    {
                        return BadRequest(new { success = false, message = "Request data is missing." });
                    }

                    string deceasedNic = SafeGetJsonProperty(reqData.JsonData, "nic") != "" ? SafeGetJsonProperty(reqData.JsonData, "nic") : SafeGetJsonProperty(reqData.JsonData, "deceasedNic");
                    if (string.IsNullOrEmpty(deceasedNic))
                    {
                        deceasedNic = SafeGetJsonProperty(reqData.JsonData, "affectedPerson");
                    }

                    if (string.IsNullOrEmpty(deceasedNic))
                    {
                        return BadRequest(new { success = false, message = "Deceased member NIC is missing." });
                    }

                    var deceasedCitizen = await _context.CitizenMasters
                        .Include(c => c.FamilyCard)
                        .FirstOrDefaultAsync(c => c.Nic == deceasedNic);

                    if (deceasedCitizen == null)
                    {
                        return BadRequest(new { success = false, message = "Deceased citizen not found in the registry." });
                    }

                    var submitterFamilyCardNumber = request.Citizen?.FamilyCardNumber;
                    if (string.IsNullOrEmpty(submitterFamilyCardNumber) || deceasedCitizen.FamilyCardNumber != submitterFamilyCardNumber)
                    {
                        return BadRequest(new { success = false, message = "Deceased citizen does not belong to this family card." });
                    }

                    if (deceasedCitizen.LifeStatus != "Alive")
                    {
                        return BadRequest(new { success = false, message = "This citizen is already marked as deceased/not alive." });
                    }

                    var familyCard = await _context.FamilyCards
                        .FirstOrDefaultAsync(fc => fc.FamilyCardNumber == submitterFamilyCardNumber);

                    if (familyCard == null || familyCard.Status != "Active")
                    {
                        return BadRequest(new { success = false, message = "The family card is invalid or inactive." });
                    }

                    // Check if a Death Certificate attachment exists
                    bool hasDeathCert = await _context.UnifiedRequestAttachments
                        .AnyAsync(a => a.RequestId == request.RequestId && a.IsActive);
                    if (!hasDeathCert)
                    {
                        return BadRequest(new { success = false, message = "Death Certificate attachment is missing." });
                    }

                    bool isDeceasedHead = deceasedCitizen.Nic == familyCard.HeadOfHouseholdNic;
                    var activeMembers = await _context.CitizenMasters
                        .Where(c => c.FamilyCardNumber == submitterFamilyCardNumber && c.Nic != deceasedCitizen.Nic && c.LifeStatus == "Alive")
                        .ToListAsync();

                    string newHeadNic = "";
                    if (isDeceasedHead && activeMembers.Count > 0)
                    {
                        newHeadNic = SafeGetJsonProperty(reqData.JsonData, "newFamilyHead");
                        if (string.IsNullOrEmpty(newHeadNic))
                        {
                            return BadRequest(new { success = false, message = "The deceased is the Family Head. A new Family Head must be assigned." });
                        }

                        var newHeadCitizen = activeMembers.FirstOrDefault(c => c.Nic == newHeadNic);
                        if (newHeadCitizen == null)
                        {
                            newHeadCitizen = activeMembers.FirstOrDefault(c => c.FullName == newHeadNic);
                        }
                        if (newHeadCitizen == null)
                        {
                            return BadRequest(new { success = false, message = "The selected new Family Head is not an active, alive member of this family card." });
                        }
                        newHeadNic = newHeadCitizen.Nic;

                        // Verify new head age (>= 18)
                        if (newHeadCitizen.Dob > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
                        {
                            return BadRequest(new { success = false, message = "The selected new Family Head must be 18 years or older." });
                        }

                        // Update database with the new Family Head
                        familyCard.HeadOfHouseholdNic = newHeadNic;
                        _context.FamilyCards.Update(familyCard);

                        newHeadCitizen.RelationshipToHead = "Head";
                        _context.CitizenMasters.Update(newHeadCitizen);

                        // Update other remaining members relative to the new head
                        foreach (var m in activeMembers)
                        {
                            if (m.Nic != newHeadNic)
                            {
                                if (m.RelationshipToHead == "Head" || m.RelationshipToHead == "Self")
                                {
                                    m.RelationshipToHead = "Member";
                                    _context.CitizenMasters.Update(m);
                                }
                            }
                        }

                        // Execute raw SQL to ensure the RelationshipToHead column is updated directly in the database
                        try
                        {
                            _context.Database.ExecuteSqlRaw(
                                "UPDATE [Citizen_Master] SET [RelationshipToHead] = 'Head' WHERE [Nic] = {0}", newHeadNic);
                            _context.Database.ExecuteSqlRaw(
                                "UPDATE [Citizen_Master] SET [RelationshipToHead] = 'Member' WHERE [FamilyCardNumber] = {0} AND [Nic] <> {1} AND ([RelationshipToHead] = 'Head' OR [RelationshipToHead] = 'Self')", 
                                submitterFamilyCardNumber, newHeadNic);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Database Update Warning] Failed to update RelationshipToHead using raw SQL: {ex.Message}");
                        }
                    }

                    // 2. Database updates for Deceased Citizen
                    deceasedCitizen.LifeStatus = "Not Alive";
                    deceasedCitizen.RelationshipToHead = null; // No longer holds a relationship role
                    _context.CitizenMasters.Update(deceasedCitizen);

                    try
                    {
                        _context.Database.ExecuteSqlRaw(
                            "UPDATE [Citizen_Master] SET [RelationshipToHead] = NULL WHERE [Nic] = {0}", deceasedCitizen.Nic);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Database Update Warning] Failed to nullify RelationshipToHead for deceased: {ex.Message}");
                    }

                    // Update Welfare Status to Inactive
                    var welfare = await _context.CitizenWelfareStatuses
                        .FirstOrDefaultAsync(w => w.CitizenNic == deceasedCitizen.Nic);
                    if (welfare != null)
                    {
                        welfare.IsActive = false;
                        _context.CitizenWelfareStatuses.Update(welfare);
                    }

                    // Deactivate Family Card if deceased was the only member
                    if (activeMembers.Count == 0)
                    {
                        familyCard.Status = "Inactive";
                        _context.FamilyCards.Update(familyCard);
                    }

                    // Update Family Member History LeftDate
                    var history = await _context.FamilyMemberHistories
                        .FirstOrDefaultAsync(h => h.CitizenNic == deceasedCitizen.Nic && h.FamilyCardNumber == submitterFamilyCardNumber && h.LeftDate == null);
                    if (history != null)
                    {
                        history.LeftDate = DateTime.UtcNow;
                        history.Reason = "Deceased";
                        _context.FamilyMemberHistories.Update(history);
                    }

                    // 3. Document/Certificate Generation
                    var template = await _context.CertificateTemplates
                        .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates
                            .FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
                    }
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
                    }
                    if (template == null)
                    {
                        throw new Exception("Active template not found.");
                    }

                    var digital = await _context.GNOfficerDigitalDetails
                        .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
                    var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
                    var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

                    if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
                    {
                        throw new Exception("GN Officer signature file is missing.");
                    }
                    if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
                    {
                        throw new Exception("GN Officer rubber stamp file is missing.");
                    }

                    var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
                    var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
                    var officerName = officer?.FullName ?? "GN Officer";

                    var verificationToken = Guid.NewGuid().ToString("N");
                    var tokenHash = HashToken(verificationToken);
                    var verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";

                    var editableValues = new CertificateEditableValuesDto
                    {
                        IssuedDate = DateTime.UtcNow.ToString("MMMM dd, yyyy")
                    };

                    var pdfBytes = RenderCertificateToPdf(
                        template.TemplateContent,
                        divisionName,
                        officerName,
                        editableValues,
                        request.RequestNumber,
                        sigPath,
                        stampPath,
                        verificationUrl
                    );

                    string fileHash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(pdfBytes);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }

                    var finalFileName = $"family_card_{submitterFamilyCardNumber}.pdf";
                    var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                    savedFilePath = Path.Combine(uploadFolder, finalFileName);

                    await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);

                    var certificateAttachment = await _context.UnifiedRequestAttachments
                        .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "GENERATED_CERTIFICATE");

                    if (certificateAttachment != null)
                    {
                        var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), certificateAttachment.FileUrl.TrimStart('/'));
                        if (oldPhysicalPath != savedFilePath && System.IO.File.Exists(oldPhysicalPath))
                        {
                            try { System.IO.File.Delete(oldPhysicalPath); } catch {}
                        }

                        certificateAttachment.FileName = finalFileName;
                        certificateAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                        certificateAttachment.UploadedAt = DateTime.UtcNow;
                        certificateAttachment.FileSize = pdfBytes.Length;
                        certificateAttachment.UploadedBy = gnNic;
                        _context.UnifiedRequestAttachments.Update(certificateAttachment);
                    }
                    else
                    {
                        certificateAttachment = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = finalFileName,
                            FileUrl = $"/uploads/requests/{finalFileName}",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "GENERATED_CERTIFICATE",
                            DisplayName = "Generated Certificate",
                            IsPrimary = true,
                            UploadedBy = gnNic,
                            FileSize = pdfBytes.Length,
                            MimeType = "application/pdf",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(certificateAttachment);
                    }
                    await _context.SaveChangesAsync();

                    var docVerification = await _context.DocumentVerifications
                        .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                    int mainAttachmentId = 0;
                    var mainAtt = await _context.UnifiedRequestAttachments.FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole != "GENERATED_CERTIFICATE");
                    if (mainAtt == null)
                    {
                        mainAtt = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = "application_form.pdf",
                            FileUrl = "/uploads/requests/dummy.pdf",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "SUPPORTING_DOCUMENT",
                            DisplayName = "Application Form",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(mainAtt);
                        await _context.SaveChangesAsync();
                    }
                    mainAttachmentId = mainAtt.AttachmentId;

                    if (docVerification == null)
                    {
                        docVerification = new DocumentVerification
                        {
                            RequestId = request.RequestId,
                            MainAttachmentId = mainAttachmentId,
                            AssignedGNOfficerNIC = gnNic,
                            VerificationStatus = "PUBLISHED",
                            VerificationRemarks = dto.Remarks ?? "Deceased member removed.",
                            VerifiedDate = DateTime.UtcNow,
                            FinalVerifiedAttachmentId = certificateAttachment.AttachmentId,
                            VerificationTokenHash = tokenHash,
                            VerificationUrl = verificationUrl,
                            OriginalFileHash = template.TemplateId.ToString(),
                            FinalFileHash = fileHash,
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow
                        };
                        _context.DocumentVerifications.Add(docVerification);
                    }
                    else
                    {
                        docVerification.MainAttachmentId = mainAttachmentId;
                        docVerification.AssignedGNOfficerNIC = gnNic;
                        docVerification.VerificationStatus = "PUBLISHED";
                        docVerification.VerificationRemarks = dto.Remarks ?? "Deceased member removed.";
                        docVerification.VerifiedDate = GetSriLankanTime();
                        docVerification.FinalVerifiedAttachmentId = certificateAttachment.AttachmentId;
                        docVerification.VerificationTokenHash = tokenHash;
                        docVerification.VerificationUrl = verificationUrl;
                        docVerification.OriginalFileHash = template.TemplateId.ToString();
                        docVerification.FinalFileHash = fileHash;
                        docVerification.UpdatedDate = GetSriLankanTime();
                        _context.DocumentVerifications.Update(docVerification);
                    }

                    request.CurrentStatus = "Accepted";
                    request.ApprovedDate = GetSriLankanTime();
                    request.LastUpdated = GetSriLankanTime();
                    _context.UnifiedRequests.Update(request);

                    var latestHistory = await _context.RequestStatusHistories
                        .Where(h => h.RequestId == request.RequestId)
                        .OrderByDescending(h => h.ChangedDate)
                        .FirstOrDefaultAsync();

                    if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                    {
                        AddStatusHistory(request.RequestId, "Accepted", dto.Remarks ?? "Deceased member removed successfully.", gnNic);
                    }

                    await SendReleaseNotification(request, gnNic);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Ok(new { success = true, message = "Deceased family member removed and family card updated successfully." });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                    {
                        try { System.IO.File.Delete(savedFilePath); } catch {}
                    }
                    var detailedError = ex.InnerException != null ? $"{ex.Message} (Inner: {ex.InnerException.Message})" : ex.Message;
                    return StatusCode(500, new { success = false, message = "Failed to process deceased member removal.", error = detailedError });
                }
            }
            else if (request.CategoryId == 4)
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                string? savedFilePath = null;
                try
                {
                    if (request.CurrentStatus == "Accepted")
                    {
                        return BadRequest(new { success = false, message = "The request has already been accepted." });
                    }

                    var familyCardNumber = request.Citizen?.FamilyCardNumber;
                    if (string.IsNullOrEmpty(familyCardNumber))
                    {
                        return BadRequest(new { success = false, message = "Applicant does not belong to a family card." });
                    }

                    var familyCard = await _context.FamilyCards
                        .FirstOrDefaultAsync(fc => fc.FamilyCardNumber == familyCardNumber);

                    if (familyCard == null || familyCard.Status != "Active")
                    {
                        return BadRequest(new { success = false, message = "The selected family card is invalid or inactive." });
                    }

                    officer = await _context.GnOfficers
                        .FirstOrDefaultAsync(o => o.Nic == gnNic);

                    if (officer == null || familyCard.GnDivisionId != officer.GnDivisionId)
                    {
                        return BadRequest(new { success = false, message = "The family card does not belong to the GN Officer's assigned division." });
                    }

                    var reqData = await _context.RequestDataEntries
                        .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);

                    if (reqData == null || string.IsNullOrEmpty(reqData.JsonData))
                    {
                        return BadRequest(new { success = false, message = "Request data is missing." });
                    }

                    string newHeadNic = SafeGetJsonProperty(reqData.JsonData, "newFamilyHead");
                    if (string.IsNullOrEmpty(newHeadNic))
                    {
                        newHeadNic = SafeGetJsonProperty(reqData.JsonData, "nic");
                    }
                    if (string.IsNullOrEmpty(newHeadNic))
                    {
                        return BadRequest(new { success = false, message = "A new Family Head must be assigned." });
                    }

                    var activeMembers = await _context.CitizenMasters
                        .Where(c => c.FamilyCardNumber == familyCardNumber && c.LifeStatus == "Alive")
                        .ToListAsync();

                    var newHeadCitizen = activeMembers.FirstOrDefault(c => c.Nic == newHeadNic);
                    if (newHeadCitizen == null)
                    {
                        newHeadCitizen = activeMembers.FirstOrDefault(c => c.FullName == newHeadNic);
                    }
                    if (newHeadCitizen == null)
                    {
                        return BadRequest(new { success = false, message = "The selected new Family Head is not an active, alive member of this family card." });
                    }
                    newHeadNic = newHeadCitizen.Nic;

                    if (newHeadCitizen.Dob > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
                    {
                        return BadRequest(new { success = false, message = "The selected new Family Head must be 18 years or older." });
                    }

                    familyCard.HeadOfHouseholdNic = newHeadNic;
                    _context.FamilyCards.Update(familyCard);

                    newHeadCitizen.RelationshipToHead = "Head";
                    _context.CitizenMasters.Update(newHeadCitizen);

                    foreach (var m in activeMembers)
                    {
                        if (m.Nic != newHeadNic)
                        {
                            if (m.RelationshipToHead == "Head" || m.RelationshipToHead == "Self")
                            {
                                m.RelationshipToHead = "Member";
                                _context.CitizenMasters.Update(m);
                            }
                        }
                    }

                    try
                    {
                        _context.Database.ExecuteSqlRaw(
                            "UPDATE [Citizen_Master] SET [RelationshipToHead] = 'Head' WHERE [Nic] = {0}", newHeadNic);
                        _context.Database.ExecuteSqlRaw(
                            "UPDATE [Citizen_Master] SET [RelationshipToHead] = 'Member' WHERE [FamilyCardNumber] = {0} AND [Nic] <> {1} AND ([RelationshipToHead] = 'Head' OR [RelationshipToHead] = 'Self')", 
                            familyCardNumber, newHeadNic);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Database Update Warning] Failed to update RelationshipToHead using raw SQL: {ex.Message}");
                    }

                    var template = await _context.CertificateTemplates
                        .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates
                            .FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
                    }
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
                    }
                    if (template == null)
                    {
                        throw new Exception("Active template not found.");
                    }

                    var digital = await _context.GNOfficerDigitalDetails
                        .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
                    var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
                    var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

                    if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
                    {
                        throw new Exception("GN Officer signature file is missing.");
                    }
                    if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
                    {
                        throw new Exception("GN Officer rubber stamp file is missing.");
                    }

                    var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
                    var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
                    var officerName = officer?.FullName ?? "GN Officer";

                    var verificationToken = Guid.NewGuid().ToString("N");
                    var tokenHash = HashToken(verificationToken);
                    var verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";

                    var editableValues = new CertificateEditableValuesDto
                    {
                        IssuedDate = DateTime.UtcNow.ToString("MMMM dd, yyyy")
                    };

                    var pdfBytes = RenderCertificateToPdf(
                        template.TemplateContent,
                        divisionName,
                        officerName,
                        editableValues,
                        request.RequestNumber,
                        sigPath,
                        stampPath,
                        verificationUrl
                    );

                    string fileHash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(pdfBytes);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }

                    var finalFileName = $"family_card_{familyCardNumber}.pdf";
                    var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                    savedFilePath = Path.Combine(uploadFolder, finalFileName);

                    await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);

                    var certificateAttachment = await _context.UnifiedRequestAttachments
                        .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "GENERATED_CERTIFICATE");

                    if (certificateAttachment != null)
                    {
                        var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), certificateAttachment.FileUrl.TrimStart('/'));
                        if (oldPhysicalPath != savedFilePath && System.IO.File.Exists(oldPhysicalPath))
                        {
                            try { System.IO.File.Delete(oldPhysicalPath); } catch {}
                        }

                        certificateAttachment.FileName = finalFileName;
                        certificateAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                        certificateAttachment.UploadedAt = DateTime.UtcNow;
                        certificateAttachment.FileSize = pdfBytes.Length;
                        certificateAttachment.UploadedBy = gnNic;
                        _context.UnifiedRequestAttachments.Update(certificateAttachment);
                    }
                    else
                    {
                        certificateAttachment = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = finalFileName,
                            FileUrl = $"/uploads/requests/{finalFileName}",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "GENERATED_CERTIFICATE",
                            DisplayName = "Generated Certificate",
                            IsPrimary = true,
                            UploadedBy = gnNic,
                            FileSize = pdfBytes.Length,
                            MimeType = "application/pdf",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(certificateAttachment);
                    }
                    await _context.SaveChangesAsync();

                    var docVerification = await _context.DocumentVerifications
                        .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                    int mainAttachmentId = 0;
                    var mainAtt = await _context.UnifiedRequestAttachments.FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole != "GENERATED_CERTIFICATE");
                    if (mainAtt == null)
                    {
                        mainAtt = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = "application_form.pdf",
                            FileUrl = "/uploads/requests/dummy.pdf",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "SUPPORTING_DOCUMENT",
                            DisplayName = "Application Form",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(mainAtt);
                        await _context.SaveChangesAsync();
                    }
                    mainAttachmentId = mainAtt.AttachmentId;

                    if (docVerification == null)
                    {
                        docVerification = new DocumentVerification
                        {
                            RequestId = request.RequestId,
                            MainAttachmentId = mainAttachmentId,
                            AssignedGNOfficerNIC = gnNic,
                            VerificationStatus = "PUBLISHED",
                            VerificationRemarks = dto.Remarks ?? "Family Head changed.",
                            VerifiedDate = DateTime.UtcNow,
                            FinalVerifiedAttachmentId = certificateAttachment.AttachmentId,
                            VerificationTokenHash = tokenHash,
                            VerificationUrl = verificationUrl,
                            OriginalFileHash = template.TemplateId.ToString(),
                            FinalFileHash = fileHash,
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow
                        };
                        _context.DocumentVerifications.Add(docVerification);
                    }
                    else
                    {
                        docVerification.MainAttachmentId = mainAttachmentId;
                        docVerification.AssignedGNOfficerNIC = gnNic;
                        docVerification.VerificationStatus = "PUBLISHED";
                        docVerification.VerificationRemarks = dto.Remarks ?? "Family Head changed.";
                        docVerification.VerifiedDate = GetSriLankanTime();
                        docVerification.FinalVerifiedAttachmentId = certificateAttachment.AttachmentId;
                        docVerification.VerificationTokenHash = tokenHash;
                        docVerification.VerificationUrl = verificationUrl;
                        docVerification.OriginalFileHash = template.TemplateId.ToString();
                        docVerification.FinalFileHash = fileHash;
                        docVerification.UpdatedDate = GetSriLankanTime();
                        _context.DocumentVerifications.Update(docVerification);
                    }

                    request.CurrentStatus = "Accepted";
                    request.ApprovedDate = GetSriLankanTime();
                    request.LastUpdated = GetSriLankanTime();
                    _context.UnifiedRequests.Update(request);

                    var latestHistory = await _context.RequestStatusHistories
                        .Where(h => h.RequestId == request.RequestId)
                        .OrderByDescending(h => h.ChangedDate)
                        .FirstOrDefaultAsync();

                    if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                    {
                        AddStatusHistory(request.RequestId, "Accepted", dto.Remarks ?? "Family Head changed successfully.", gnNic);
                    }

                    await SendReleaseNotification(request, gnNic);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Ok(new { success = true, message = "Family Head changed and family card updated successfully." });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                    {
                        try { System.IO.File.Delete(savedFilePath); } catch {}
                    }
                    var detailedError = ex.InnerException != null ? $"{ex.Message} (Inner: {ex.InnerException.Message})" : ex.Message;
                    return StatusCode(500, new { success = false, message = "Failed to process Family Head change.", error = detailedError });
                }
            }
            else if (request.CategoryId == 42)
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                string? savedFilePath = null;
                try
                {
                    var dbRequest = await _context.UnifiedRequests.FirstOrDefaultAsync(r => r.RequestId == request.RequestId);
                    if (dbRequest == null)
                    {
                        return BadRequest(new { success = false, message = "Request not found." });
                    }
                    if (dbRequest.CurrentStatus == "Accepted" || dbRequest.CurrentStatus == "ACCEPTED")
                    {
                        return BadRequest(new { success = false, message = "The request has already been accepted." });
                    }

                    officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
                    if (officer == null)
                    {
                        return BadRequest(new { success = false, message = "Authenticated GN Officer record not found." });
                    }

                    var reqData = await _context.RequestDataEntries
                        .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);

                    if (reqData == null || string.IsNullOrEmpty(reqData.JsonData))
                    {
                        return BadRequest(new { success = false, message = "Request data is missing." });
                    }

                    string destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "newGnDivision");
                    if (string.IsNullOrEmpty(destGnDivisionId))
                    {
                        destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivisionId");
                    }
                    if (string.IsNullOrEmpty(destGnDivisionId))
                    {
                        destGnDivisionId = request.GnDivision ?? string.Empty;
                    }

                    var destinationDivision = await _context.GnDivisions
                        .FirstOrDefaultAsync(d => d.Id == destGnDivisionId);
                    if (destinationDivision == null)
                    {
                        return BadRequest(new { success = false, message = "Destination GN Division not found." });
                    }

                    var destGnOfficer = await _context.GnOfficers
                        .FirstOrDefaultAsync(o => o.GnDivisionId == destGnDivisionId);
                    if (destGnOfficer == null)
                    {
                        return BadRequest(new { success = false, message = "No GN Officer assigned to the destination GN Division." });
                    }

                    var applicant = await _context.CitizenMasters
                        .Include(c => c.FamilyCard)
                        .FirstOrDefaultAsync(c => c.Nic == request.CitizenNic);

                    if (applicant == null)
                    {
                        return BadRequest(new { success = false, message = $"Applicant citizen with NIC {request.CitizenNic} not found." });
                    }

                    if (string.IsNullOrEmpty(applicant.FamilyCardNumber) || applicant.FamilyCard?.Status != "Active")
                    {
                        return BadRequest(new { success = false, message = "Applicant does not belong to an active family card." });
                    }

                    string familyCardNumber = applicant.FamilyCardNumber;
                    var familyCard = await _context.FamilyCards
                        .FirstOrDefaultAsync(fc => fc.FamilyCardNumber == familyCardNumber);

                    if (familyCard == null)
                    {
                        return BadRequest(new { success = false, message = $"Family card {familyCardNumber} not found." });
                    }

                    var familyMembers = await _context.CitizenMasters
                        .Where(c => c.FamilyCardNumber == familyCardNumber && c.LifeStatus == "Alive")
                        .ToListAsync();

                    string prevGnDivisionId = familyCard.GnDivisionId;
                    
                    var newAddress = SafeGetJsonProperty(reqData.JsonData, "newResidentialAddress");
                    if (string.IsNullOrEmpty(newAddress))
                    {
                        newAddress = SafeGetJsonProperty(reqData.JsonData, "newAddress");
                    }
                    if (string.IsNullOrEmpty(newAddress))
                    {
                        newAddress = familyCard.Address;
                    }

                    var newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "newHouseNumber");
                    if (string.IsNullOrEmpty(newHouseNumber))
                    {
                        newHouseNumber = familyCard.HouseNumber;
                    }

                    // Update Family Card details
                    familyCard.GnDivisionId = destGnDivisionId;
                    familyCard.Address = newAddress;
                    familyCard.HouseNumber = !string.IsNullOrEmpty(newHouseNumber) ? newHouseNumber : null;
                    _context.FamilyCards.Update(familyCard);

                    // Update GnDivisionId for all members and save their details for history logging
                    var prevDetails = new Dictionary<string, (string Division, string Relationship)>();

                    foreach (var member in familyMembers)
                    {
                        prevDetails[member.Nic] = (
                            member.GnDivisionId ?? string.Empty,
                            member.RelationshipToHead ?? string.Empty
                        );

                        if (member.GnDivisionId != destGnDivisionId)
                        {
                            member.GnDivisionId = destGnDivisionId;
                            _context.CitizenMasters.Update(member);
                        }
                    }
                    await _context.SaveChangesAsync();

                    // Record movement histories in Family_Member_History
                    foreach (var member in familyMembers)
                    {
                        var details = prevDetails[member.Nic];
                        var history = new FamilyMemberHistory
                        {
                            CitizenNic = member.Nic,
                            FamilyCardNumber = familyCardNumber,
                            JoinedDate = GetSriLankanTime(),
                            LeftDate = null,
                            Reason = "GN Division Transfer",
                            PreviousFamilyCard = familyCardNumber,
                            PreviousGnDivision = details.Division,
                            DestinationGnDivision = destGnDivisionId,
                            PreviousRelationship = details.Relationship,
                            NewRelationship = details.Relationship,
                            RequestId = request.RequestId,
                            ActionType = "GnTransfer",
                            ChangedBy = gnNic,
                            ChangedDate = GetSriLankanTime(),
                            Remarks = $"family relocated to new GN division {destGnDivisionId} under category 42 approval"
                        };
                        _context.FamilyMemberHistories.Add(history);
                    }
                    await _context.SaveChangesAsync();

                    // Reassign division/officer and handle open requests/documents separately for each family member
                    foreach (var member in familyMembers)
                    {
                        var details = prevDetails[member.Nic];
                        if (details.Division != destGnDivisionId)
                        {
                            await _citizenRequestService.HandleCitizenGnDivisionChangeAsync(
                                member.Nic,
                                details.Division,
                                destGnDivisionId,
                                string.Empty, // Pass empty string so family card is not deactivated
                                destGnOfficer.Nic,
                                gnNic,
                                request.RequestId
                            );
                        }
                    }

                    bool isCrossGn = prevGnDivisionId != destGnDivisionId;
                    if (isCrossGn)
                    {
                        request.CurrentStatus = "Accepted";
                        request.ApprovedDate = GetSriLankanTime();
                        request.LastUpdated = GetSriLankanTime();
                        _context.UnifiedRequests.Update(request);

                        var latestCrossGnHistory = await _context.RequestStatusHistories
                            .Where(h => h.RequestId == request.RequestId)
                            .OrderByDescending(h => h.ChangedDate)
                            .FirstOrDefaultAsync();

                        if (latestCrossGnHistory == null || latestCrossGnHistory.Status.ToUpper() != "ACCEPTED")
                        {
                            AddStatusHistory(request.RequestId, "Accepted", dto.Remarks, gnNic);
                        }

                        await SendReleaseNotification(request, gnNic);
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();

                        return Ok(new { success = true, message = "GN Division Transfer approved and Family Card updated successfully." });
                    }

                    // Generate Certificate and Verification PDF
                    var template = await _context.CertificateTemplates
                        .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
                    }
                    if (template == null)
                    {
                        throw new Exception("Active template not found.");
                    }

                    var digital = await _context.GNOfficerDigitalDetails
                        .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
                    var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
                    var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

                    if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
                    {
                        throw new Exception("GN Officer signature file is missing.");
                    }
                    if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
                    {
                        throw new Exception("GN Officer rubber stamp file is missing.");
                    }

                    var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
                    var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
                    var officerName = officer?.FullName ?? "GN Officer";

                    var verificationToken = Guid.NewGuid().ToString("N");
                    var tokenHash = HashToken(verificationToken);
                    var verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";

                    var editableValues = new CertificateEditableValuesDto
                    {
                        IssuedDate = DateTime.UtcNow.ToString("MMMM dd, yyyy")
                    };

                    var pdfBytes = RenderCertificateToPdf(
                        template.TemplateContent,
                        divisionName,
                        officerName,
                        editableValues,
                        request.RequestNumber,
                        sigPath,
                        stampPath,
                        verificationUrl
                    );

                    string fileHash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(pdfBytes);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }

                    var finalFileName = $"family_card_{familyCardNumber}.pdf";
                    var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                    savedFilePath = Path.Combine(uploadFolder, finalFileName);

                    await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);

                    var certificateAttachment = await _context.UnifiedRequestAttachments
                        .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "GENERATED_CERTIFICATE");

                    if (certificateAttachment != null)
                    {
                        var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), certificateAttachment.FileUrl.TrimStart('/'));
                        if (oldPhysicalPath != savedFilePath && System.IO.File.Exists(oldPhysicalPath))
                        {
                            try { System.IO.File.Delete(oldPhysicalPath); } catch {}
                        }

                        certificateAttachment.FileName = finalFileName;
                        certificateAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                        certificateAttachment.UploadedAt = DateTime.UtcNow;
                        certificateAttachment.FileSize = pdfBytes.Length;
                        certificateAttachment.UploadedBy = gnNic;
                        _context.UnifiedRequestAttachments.Update(certificateAttachment);
                    }
                    else
                    {
                        certificateAttachment = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = finalFileName,
                            FileUrl = $"/uploads/requests/{finalFileName}",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "GENERATED_CERTIFICATE",
                            DisplayName = "Generated Certificate",
                            IsPrimary = true,
                            UploadedBy = gnNic,
                            FileSize = pdfBytes.Length,
                            MimeType = "application/pdf",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(certificateAttachment);
                    }
                    await _context.SaveChangesAsync();

                    // Save verification entry
                    var docVerification = await _context.DocumentVerifications
                        .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                    int mainAttachmentId = 0;
                    var mainAtt = await _context.UnifiedRequestAttachments.FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole != "GENERATED_CERTIFICATE");
                    if (mainAtt == null)
                    {
                        mainAtt = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = "application_form.pdf",
                            FileUrl = "/uploads/requests/dummy.pdf",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "SUPPORTING_DOCUMENT",
                            DisplayName = "Application Form",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(mainAtt);
                        await _context.SaveChangesAsync();
                    }
                    mainAttachmentId = mainAtt.AttachmentId;

                    if (docVerification == null)
                    {
                        docVerification = new DocumentVerification
                        {
                            RequestId = request.RequestId,
                            MainAttachmentId = mainAttachmentId,
                            AssignedGNOfficerNIC = gnNic,
                            VerificationStatus = "PUBLISHED",
                            VerificationRemarks = dto.Remarks ?? "GN Division Transfer approved.",
                            VerifiedDate = DateTime.UtcNow,
                            FinalVerifiedAttachmentId = certificateAttachment.AttachmentId,
                            VerificationTokenHash = tokenHash,
                            VerificationUrl = verificationUrl,
                            OriginalFileHash = template.TemplateId.ToString(),
                            FinalFileHash = fileHash,
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow
                        };
                        _context.DocumentVerifications.Add(docVerification);
                    }
                    else
                    {
                        docVerification.MainAttachmentId = mainAttachmentId;
                        docVerification.AssignedGNOfficerNIC = gnNic;
                        docVerification.VerificationStatus = "PUBLISHED";
                        docVerification.VerificationRemarks = dto.Remarks ?? "GN Division Transfer approved.";
                        docVerification.VerifiedDate = GetSriLankanTime();
                        docVerification.FinalVerifiedAttachmentId = certificateAttachment.AttachmentId;
                        docVerification.VerificationTokenHash = tokenHash;
                        docVerification.VerificationUrl = verificationUrl;
                        docVerification.OriginalFileHash = template.TemplateId.ToString();
                        docVerification.FinalFileHash = fileHash;
                        docVerification.UpdatedDate = GetSriLankanTime();
                        _context.DocumentVerifications.Update(docVerification);
                    }

                    request.CurrentStatus = "Accepted";
                    request.ApprovedDate = GetSriLankanTime();
                    request.LastUpdated = GetSriLankanTime();
                    _context.UnifiedRequests.Update(request);

                    var latestHistory = await _context.RequestStatusHistories
                        .Where(h => h.RequestId == request.RequestId)
                        .OrderByDescending(h => h.ChangedDate)
                        .FirstOrDefaultAsync();

                    if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                    {
                        AddStatusHistory(request.RequestId, "Accepted", dto.Remarks, gnNic);
                    }

                    await SendReleaseNotification(request, gnNic);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Ok(new { success = true, message = "GN Division Transfer approved and Family Card updated successfully." });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                    {
                        try { System.IO.File.Delete(savedFilePath); } catch {}
                    }
                    var detailedError = ex.InnerException != null ? $"{ex.Message} (Inner: {ex.InnerException.Message})" : ex.Message;
                    return StatusCode(500, new { success = false, message = "Failed to process GN Division Transfer request.", error = detailedError });
                }
            }
            else if (request.CategoryId == 5)
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                string? savedFilePath = null;
                try
                {
                    var dbRequest = await _context.UnifiedRequests.FirstOrDefaultAsync(r => r.RequestId == request.RequestId);
                    if (dbRequest == null)
                    {
                        return BadRequest(new { success = false, message = "Request not found." });
                    }
                    if (dbRequest.CurrentStatus == "Accepted" || dbRequest.CurrentStatus == "ACCEPTED")
                    {
                        return BadRequest(new { success = false, message = "The request has already been accepted." });
                    }

                    officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
                    if (officer == null)
                    {
                        return BadRequest(new { success = false, message = "Authenticated GN Officer record not found." });
                    }

                    var reqData = await _context.RequestDataEntries
                        .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);

                    if (reqData == null || string.IsNullOrEmpty(reqData.JsonData))
                    {
                        return BadRequest(new { success = false, message = "Request data is missing." });
                    }

                    string requestType = SafeGetJsonProperty(reqData.JsonData, "requestType");
                    if (string.IsNullOrEmpty(requestType))
                    {
                        requestType = "Marriage";
                    }

                    string newFamilyCardNumber = string.Empty;
                    string destGnDivisionId = string.Empty;
                    GnOfficer destGnOfficer;

                    if (string.Equals(requestType, "Marriage", StringComparison.OrdinalIgnoreCase))
                    {
                        string spouseNic = SafeGetJsonProperty(reqData.JsonData, "spouseNic");
                        if (string.IsNullOrEmpty(spouseNic))
                        {
                            return BadRequest(new { success = false, message = "Spouse NIC is missing in the request." });
                        }

                        if (string.Equals(request.CitizenNic, spouseNic))
                        {
                            return BadRequest(new { success = false, message = "Applicant and Spouse cannot be the same citizen." });
                        }

                        var applicant = await _context.CitizenMasters
                            .Include(c => c.FamilyCard)
                            .FirstOrDefaultAsync(c => c.Nic == request.CitizenNic);
                        var spouse = await _context.CitizenMasters
                            .Include(c => c.FamilyCard)
                            .FirstOrDefaultAsync(c => c.Nic == spouseNic);

                        if (applicant == null)
                        {
                            return BadRequest(new { success = false, message = $"Applicant citizen with NIC {request.CitizenNic} not found." });
                        }
                        if (spouse == null)
                        {
                            return BadRequest(new { success = false, message = $"Spouse citizen with NIC {spouseNic} not found." });
                        }

                        if (applicant.LifeStatus != "Alive")
                        {
                            return BadRequest(new { success = false, message = "Applicant is not active/alive." });
                        }
                        if (spouse.LifeStatus != "Alive")
                        {
                            return BadRequest(new { success = false, message = "Spouse is not active/alive." });
                        }

                        if (string.IsNullOrEmpty(applicant.FamilyCardNumber) || applicant.FamilyCard?.Status != "Active")
                        {
                            return BadRequest(new { success = false, message = "Applicant does not belong to an active family card." });
                        }
                        if (string.IsNullOrEmpty(spouse.FamilyCardNumber) || spouse.FamilyCard?.Status != "Active")
                        {
                            return BadRequest(new { success = false, message = "Spouse does not belong to an active family card." });
                        }

                        string sameGnDivisionVal = SafeGetJsonProperty(reqData.JsonData, "sameGnDivision");
                        bool isSameGn = string.Equals(sameGnDivisionVal, "Yes", StringComparison.OrdinalIgnoreCase) || 
                                        string.Equals(sameGnDivisionVal, "Y", StringComparison.OrdinalIgnoreCase);

                        if (isSameGn)
                        {
                            destGnDivisionId = applicant.GnDivisionId;
                        }
                        else
                        {
                            destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivisionId");
                            if (string.IsNullOrEmpty(destGnDivisionId))
                            {
                                destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "newGnDivision");
                            }
                            if (string.IsNullOrEmpty(destGnDivisionId))
                            {
                                destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivision");
                            }
                            if (string.IsNullOrEmpty(destGnDivisionId))
                            {
                                destGnDivisionId = request.GnDivision ?? string.Empty;
                            }
                        }

                        var destinationDivision = await _context.GnDivisions
                            .FirstOrDefaultAsync(d => d.Id == destGnDivisionId);
                        if (destinationDivision == null)
                        {
                            return BadRequest(new { success = false, message = "Destination GN Division not found." });
                        }

                        var resolvedOfficer = await _context.GnOfficers
                            .FirstOrDefaultAsync(o => o.GnDivisionId == destGnDivisionId);
                        if (resolvedOfficer == null)
                        {
                            return BadRequest(new { success = false, message = "No GN Officer assigned to the destination GN Division." });
                        }
                        destGnOfficer = resolvedOfficer;

                        // Head of Household determination
                        string newHeadOfHouseholdNic = SafeGetJsonProperty(reqData.JsonData, "newHeadOfHousehold");

                        CitizenMaster headCitizen;
                        CitizenMaster nonHeadCitizen;

                        bool applicantIsMale = string.Equals(applicant.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                        bool spouseIsMale = string.Equals(spouse.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                        bool applicantIsFemale = string.Equals(applicant.Gender, "Female", StringComparison.OrdinalIgnoreCase);
                        bool spouseIsFemale = string.Equals(spouse.Gender, "Female", StringComparison.OrdinalIgnoreCase);

                        if (applicantIsFemale && spouseIsMale && string.Equals(newHeadOfHouseholdNic, applicant.Nic))
                        {
                            headCitizen = applicant;
                            nonHeadCitizen = spouse;
                            headCitizen.RelationshipToHead = "Head";
                            nonHeadCitizen.RelationshipToHead = "Husband";
                        }
                        else if (spouseIsFemale && applicantIsMale && string.Equals(newHeadOfHouseholdNic, spouse.Nic))
                        {
                            headCitizen = spouse;
                            nonHeadCitizen = applicant;
                            headCitizen.RelationshipToHead = "Head";
                            nonHeadCitizen.RelationshipToHead = "Husband";
                        }
                        else if (applicantIsMale)
                        {
                            headCitizen = applicant;
                            nonHeadCitizen = spouse;
                            headCitizen.RelationshipToHead = "Head";
                            nonHeadCitizen.RelationshipToHead = spouseIsFemale ? "Wife" : "Spouse";
                        }
                        else if (spouseIsMale)
                        {
                            headCitizen = spouse;
                            nonHeadCitizen = applicant;
                            headCitizen.RelationshipToHead = "Head";
                            nonHeadCitizen.RelationshipToHead = applicantIsFemale ? "Wife" : "Spouse";
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(newHeadOfHouseholdNic) && string.Equals(newHeadOfHouseholdNic, spouse.Nic))
                            {
                                headCitizen = spouse;
                                nonHeadCitizen = applicant;
                            }
                            else
                            {
                                headCitizen = applicant;
                                nonHeadCitizen = spouse;
                            }
                            headCitizen.RelationshipToHead = "Head";
                            nonHeadCitizen.RelationshipToHead = "Spouse";
                        }

                        string newHeadOfHouseholdNicFinal = headCitizen.Nic;

                        // Generate Unique FamilyCardNumber based on division numeric part (e.g. 60) and 2-digit sequence (e.g. 01, 02)
                        var digits = new string((destGnDivisionId ?? string.Empty).Where(char.IsDigit).ToArray());
                        if (string.IsNullOrEmpty(digits))
                        {
                            digits = "10";
                        }
                        
                        int seq = 1;
                        while (true)
                        {
                            string testCardNumber = $"FC-{digits}{seq:D2}";
                            bool exists = await _context.FamilyCards.AnyAsync(fc => fc.FamilyCardNumber == testCardNumber)
                                          || string.Equals(testCardNumber, applicant.FamilyCardNumber, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(testCardNumber, spouse.FamilyCardNumber, StringComparison.OrdinalIgnoreCase);
                            
                            if (!exists)
                            {
                                newFamilyCardNumber = testCardNumber;
                                break;
                            }
                            seq++;
                        }

                        var newAddress = SafeGetJsonProperty(reqData.JsonData, "newAddress");
                        if (string.IsNullOrEmpty(newAddress))
                        {
                            newAddress = SafeGetJsonProperty(reqData.JsonData, "address");
                        }
                        if (string.IsNullOrEmpty(newAddress))
                        {
                            newAddress = "No Address Provided";
                        }

                        var newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "houseNumber");
                        if (string.IsNullOrEmpty(newHouseNumber))
                        {
                            newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "newHouseNumber");
                        }

                        // Create Family Card
                        var newFamilyCard = new FamilyCard
                        {
                            FamilyCardNumber = newFamilyCardNumber,
                            HeadOfHouseholdNic = newHeadOfHouseholdNicFinal,
                            GnDivisionId = destGnDivisionId,
                            Address = newAddress,
                            RegisteredDate = DateOnly.FromDateTime(GetSriLankanTime()),
                            Status = "Active",
                            HouseNumber = !string.IsNullOrEmpty(newHouseNumber) ? newHouseNumber : null
                        };
                        _context.FamilyCards.Add(newFamilyCard);
                        await _context.SaveChangesAsync();

                        // Field Preservation
                        string prevApplicantCard = applicant.FamilyCardNumber ?? string.Empty;
                        string prevSpouseCard = spouse.FamilyCardNumber ?? string.Empty;
                        string prevApplicantGn = applicant.GnDivisionId ?? string.Empty;
                        string prevSpouseGn = spouse.GnDivisionId ?? string.Empty;
                        string prevApplicantRelation = applicant.RelationshipToHead ?? string.Empty;
                        string prevSpouseRelation = spouse.RelationshipToHead ?? string.Empty;

                        // Update applicant and spouse in Citizen_Master
                        headCitizen.PastFamilyCardNumber = (headCitizen.Nic == applicant.Nic) ? prevApplicantCard : prevSpouseCard;
                        headCitizen.FamilyCardNumber = newFamilyCardNumber;
                        headCitizen.MaritalStatus = "Married";
                        headCitizen.RelationshipToHead = "Head";
                        if (headCitizen.GnDivisionId != destGnDivisionId)
                        {
                            headCitizen.GnDivisionId = destGnDivisionId;
                        }

                        nonHeadCitizen.PastFamilyCardNumber = (nonHeadCitizen.Nic == applicant.Nic) ? prevApplicantCard : prevSpouseCard;
                        nonHeadCitizen.FamilyCardNumber = newFamilyCardNumber;
                        nonHeadCitizen.MaritalStatus = "Married";
                        if (nonHeadCitizen.GnDivisionId != destGnDivisionId)
                        {
                            nonHeadCitizen.GnDivisionId = destGnDivisionId;
                        }

                        _context.CitizenMasters.Update(headCitizen);
                        _context.CitizenMasters.Update(nonHeadCitizen);
                        await _context.SaveChangesAsync();

                        // Add movement records to Family_Member_History
                        var headHistory = new FamilyMemberHistory
                        {
                            CitizenNic = headCitizen.Nic,
                            FamilyCardNumber = newFamilyCardNumber,
                            JoinedDate = GetSriLankanTime(),
                            LeftDate = null,
                            Reason = "Marriage",
                            PreviousFamilyCard = (headCitizen.Nic == applicant.Nic) ? prevApplicantCard : prevSpouseCard,
                            PreviousGnDivision = (headCitizen.Nic == applicant.Nic) ? prevApplicantGn : prevSpouseGn,
                            DestinationGnDivision = destGnDivisionId,
                            PreviousRelationship = (headCitizen.Nic == applicant.Nic) ? prevApplicantRelation : prevSpouseRelation,
                            NewRelationship = "Head",
                            RequestId = request.RequestId,
                            ActionType = "MarriageTransfer",
                            ChangedBy = gnNic,
                            ChangedDate = GetSriLankanTime(),
                            Remarks = "moved to a newly created family card after marriage approval"
                        };

                        var nonHeadHistory = new FamilyMemberHistory
                        {
                            CitizenNic = nonHeadCitizen.Nic,
                            FamilyCardNumber = newFamilyCardNumber,
                            JoinedDate = GetSriLankanTime(),
                            LeftDate = null,
                            Reason = "Marriage",
                            PreviousFamilyCard = (nonHeadCitizen.Nic == applicant.Nic) ? prevApplicantCard : prevSpouseCard,
                            PreviousGnDivision = (nonHeadCitizen.Nic == applicant.Nic) ? prevApplicantGn : prevSpouseGn,
                            DestinationGnDivision = destGnDivisionId,
                            PreviousRelationship = (nonHeadCitizen.Nic == applicant.Nic) ? prevApplicantRelation : prevSpouseRelation,
                            NewRelationship = nonHeadCitizen.RelationshipToHead,
                            RequestId = request.RequestId,
                            ActionType = "MarriageTransfer",
                            ChangedBy = gnNic,
                            ChangedDate = GetSriLankanTime(),
                            Remarks = "moved to a newly created family card after marriage approval"
                        };

                        _context.FamilyMemberHistories.Add(headHistory);
                        _context.FamilyMemberHistories.Add(nonHeadHistory);
                        await _context.SaveChangesAsync();

                        // Check both previous family cards after moving the citizens
                        if (!string.IsNullOrEmpty(prevApplicantCard))
                        {
                            var remainingAlive = await _context.CitizenMasters
                                .CountAsync(c => c.FamilyCardNumber == prevApplicantCard && c.LifeStatus == "Alive" && c.Nic != applicant.Nic && c.Nic != spouse.Nic);

                            if (remainingAlive == 0)
                            {
                                var oldCard = await _context.FamilyCards.FirstOrDefaultAsync(fc => fc.FamilyCardNumber == prevApplicantCard);
                                if (oldCard != null)
                                {
                                    oldCard.Status = "Inactive";
                                    _context.FamilyCards.Update(oldCard);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(prevSpouseCard) && prevSpouseCard != prevApplicantCard)
                        {
                            var remainingAlive = await _context.CitizenMasters
                                .CountAsync(c => c.FamilyCardNumber == prevSpouseCard && c.LifeStatus == "Alive" && c.Nic != applicant.Nic && c.Nic != spouse.Nic);

                            if (remainingAlive == 0)
                            {
                                var oldCard = await _context.FamilyCards.FirstOrDefaultAsync(fc => fc.FamilyCardNumber == prevSpouseCard);
                                if (oldCard != null)
                                {
                                    oldCard.Status = "Inactive";
                                    _context.FamilyCards.Update(oldCard);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();

                        // Reassign division/officer and handle open requests/documents for applicant if division is different
                        if (prevApplicantGn != destGnDivisionId)
                        {
                            await _citizenRequestService.HandleCitizenGnDivisionChangeAsync(
                                applicant.Nic,
                                prevApplicantGn ?? string.Empty,
                                destGnDivisionId,
                                prevApplicantCard ?? string.Empty,
                                destGnOfficer.Nic,
                                gnNic,
                                request.RequestId
                            );
                        }

                        // Reassign division/officer and handle open requests/documents for spouse if division is different
                        if (prevSpouseGn != destGnDivisionId)
                        {
                            await _citizenRequestService.HandleCitizenGnDivisionChangeAsync(
                                spouse.Nic,
                                prevSpouseGn ?? string.Empty,
                                destGnDivisionId,
                                prevSpouseCard ?? string.Empty,
                                destGnOfficer.Nic,
                                gnNic,
                                request.RequestId
                            );
                        }
                    }
                    else if (string.Equals(requestType, "DivorceOrSeparation", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(requestType, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        var memberNicsToMove = SafeGetJsonArray(reqData.JsonData, "familyMembersToMove");
                        if (memberNicsToMove == null || memberNicsToMove.Count == 0)
                        {
                            return BadRequest(new { success = false, message = "No family members selected to move." });
                        }

                        var movingCitizens = await _context.CitizenMasters
                            .Include(c => c.FamilyCard)
                            .Where(c => memberNicsToMove.Contains(c.Nic))
                            .ToListAsync();

                        var applicant = await _context.CitizenMasters
                            .Include(c => c.FamilyCard)
                            .FirstOrDefaultAsync(c => c.Nic == request.CitizenNic);

                        if (applicant == null)
                        {
                            return BadRequest(new { success = false, message = $"Applicant citizen with NIC {request.CitizenNic} not found." });
                        }

                        foreach (var nic in memberNicsToMove)
                        {
                            var citizen = movingCitizens.FirstOrDefault(c => c.Nic == nic);
                            if (citizen == null)
                            {
                                return BadRequest(new { success = false, message = $"Selected family member with NIC {nic} not found." });
                            }
                            if (citizen.LifeStatus != "Alive")
                            {
                                return BadRequest(new { success = false, message = $"Selected family member with NIC {nic} is not alive." });
                            }
                            if (string.IsNullOrEmpty(citizen.FamilyCardNumber) || citizen.FamilyCard?.Status != "Active")
                            {
                                return BadRequest(new { success = false, message = $"Selected family member with NIC {nic} does not belong to an active family card." });
                            }
                        }

                        string sameGnDivisionVal = SafeGetJsonProperty(reqData.JsonData, "sameGnDivision");
                        bool isSameGn = string.Equals(sameGnDivisionVal, "Yes", StringComparison.OrdinalIgnoreCase) || 
                                        string.Equals(sameGnDivisionVal, "Y", StringComparison.OrdinalIgnoreCase);

                        if (isSameGn)
                        {
                            destGnDivisionId = applicant.GnDivisionId;
                        }
                        else
                        {
                            destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivisionId");
                            if (string.IsNullOrEmpty(destGnDivisionId))
                            {
                                destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "newGnDivision");
                            }
                            if (string.IsNullOrEmpty(destGnDivisionId))
                            {
                                destGnDivisionId = SafeGetJsonProperty(reqData.JsonData, "gnDivision");
                            }
                            if (string.IsNullOrEmpty(destGnDivisionId))
                            {
                                destGnDivisionId = request.GnDivision ?? string.Empty;
                            }
                        }

                        var destinationDivision = await _context.GnDivisions
                            .FirstOrDefaultAsync(d => d.Id == destGnDivisionId);
                        if (destinationDivision == null)
                        {
                            return BadRequest(new { success = false, message = "Destination GN Division not found." });
                        }

                        var resolvedOfficer = await _context.GnOfficers
                            .FirstOrDefaultAsync(o => o.GnDivisionId == destGnDivisionId);
                        if (resolvedOfficer == null)
                        {
                            return BadRequest(new { success = false, message = "No GN Officer assigned to the destination GN Division." });
                        }
                        destGnOfficer = resolvedOfficer;

                        string newHeadOfHouseholdNic = SafeGetJsonProperty(reqData.JsonData, "newHeadOfHousehold");
                        var headCitizen = movingCitizens.FirstOrDefault(c => c.Nic == newHeadOfHouseholdNic) ?? movingCitizens[0];

                        // Generate Unique FamilyCardNumber based on division numeric part (e.g. 60) and 2-digit sequence (e.g. 01, 02)
                        var digits = new string((destGnDivisionId ?? string.Empty).Where(char.IsDigit).ToArray());
                        if (string.IsNullOrEmpty(digits))
                        {
                            digits = "10";
                        }
                        
                        int seq = 1;
                        while (true)
                        {
                            string testCardNumber = $"FC-{digits}{seq:D2}";
                            bool exists = await _context.FamilyCards.AnyAsync(fc => fc.FamilyCardNumber == testCardNumber)
                                          || movingCitizens.Any(c => string.Equals(testCardNumber, c.FamilyCardNumber, StringComparison.OrdinalIgnoreCase));
                            
                            if (!exists)
                            {
                                newFamilyCardNumber = testCardNumber;
                                break;
                            }
                            seq++;
                        }

                        var newAddress = SafeGetJsonProperty(reqData.JsonData, "newAddress");
                        if (string.IsNullOrEmpty(newAddress))
                        {
                            newAddress = SafeGetJsonProperty(reqData.JsonData, "address");
                        }
                        if (string.IsNullOrEmpty(newAddress))
                        {
                            newAddress = "No Address Provided";
                        }

                        var newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "houseNumber");
                        if (string.IsNullOrEmpty(newHouseNumber))
                        {
                            newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "newHouseNumber");
                        }

                        // Create Family Card
                        var newFamilyCard = new FamilyCard
                        {
                            FamilyCardNumber = newFamilyCardNumber,
                            HeadOfHouseholdNic = headCitizen.Nic,
                            GnDivisionId = destGnDivisionId,
                            Address = newAddress,
                            RegisteredDate = DateOnly.FromDateTime(GetSriLankanTime()),
                            Status = "Active",
                            HouseNumber = !string.IsNullOrEmpty(newHouseNumber) ? newHouseNumber : null
                        };
                        _context.FamilyCards.Add(newFamilyCard);
                        await _context.SaveChangesAsync();

                        // Keep track of moving citizens' previous values
                        var prevDetails = new Dictionary<string, (string Card, string Division, string Relationship)>();

                        foreach (var citizen in movingCitizens)
                        {
                            prevDetails[citizen.Nic] = (
                                citizen.FamilyCardNumber ?? string.Empty, 
                                citizen.GnDivisionId ?? string.Empty, 
                                citizen.RelationshipToHead ?? string.Empty
                            );

                            citizen.PastFamilyCardNumber = citizen.FamilyCardNumber;
                            citizen.FamilyCardNumber = newFamilyCardNumber;

                            if (citizen.Nic == headCitizen.Nic)
                            {
                                citizen.RelationshipToHead = "Head";
                            }
                            else
                            {
                                if (citizen.RelationshipToHead == "Head" || string.IsNullOrEmpty(citizen.RelationshipToHead))
                                {
                                    citizen.RelationshipToHead = "Member";
                                }
                            }

                            if (citizen.GnDivisionId != destGnDivisionId)
                            {
                                citizen.GnDivisionId = destGnDivisionId;
                            }

                            _context.CitizenMasters.Update(citizen);
                        }
                        await _context.SaveChangesAsync();

                        // Record movement histories in Family_Member_History
                        foreach (var citizen in movingCitizens)
                        {
                            var details = prevDetails[citizen.Nic];
                            var history = new FamilyMemberHistory
                            {
                                CitizenNic = citizen.Nic,
                                FamilyCardNumber = newFamilyCardNumber,
                                JoinedDate = GetSriLankanTime(),
                                LeftDate = null,
                                Reason = requestType,
                                PreviousFamilyCard = details.Card,
                                PreviousGnDivision = details.Division,
                                DestinationGnDivision = destGnDivisionId,
                                PreviousRelationship = details.Relationship,
                                NewRelationship = citizen.RelationshipToHead,
                                RequestId = request.RequestId,
                                ActionType = "FamilyTransfer",
                                ChangedBy = gnNic,
                                ChangedDate = GetSriLankanTime(),
                                Remarks = $"moved to a newly created family card due to {requestType} approval"
                            };
                            _context.FamilyMemberHistories.Add(history);
                        }
                        await _context.SaveChangesAsync();

                        // Deactivate old cards that are left empty (0 remaining alive members)
                        var leftCardNumbers = prevDetails.Values
                            .Select(v => v.Card)
                            .Where(c => !string.IsNullOrEmpty(c))
                            .Distinct()
                            .ToList();

                        foreach (var oldCardNo in leftCardNumbers)
                        {
                            var remainingAlive = await _context.CitizenMasters
                                .CountAsync(c => c.FamilyCardNumber == oldCardNo && c.LifeStatus == "Alive" && !memberNicsToMove.Contains(c.Nic));

                            if (remainingAlive == 0)
                            {
                                var oldCard = await _context.FamilyCards.FirstOrDefaultAsync(fc => fc.FamilyCardNumber == oldCardNo);
                                if (oldCard != null)
                                {
                                    oldCard.Status = "Inactive";
                                    _context.FamilyCards.Update(oldCard);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();

                        // Reassign division/officer and handle open requests/documents separately for each citizen if division is different
                        foreach (var citizen in movingCitizens)
                        {
                            var details = prevDetails[citizen.Nic];
                            if (details.Division != destGnDivisionId)
                            {
                                await _citizenRequestService.HandleCitizenGnDivisionChangeAsync(
                                    citizen.Nic,
                                    details.Division,
                                    destGnDivisionId,
                                    details.Card,
                                    destGnOfficer.Nic,
                                    gnNic,
                                    request.RequestId
                                );
                            }
                        }
                    }

                    // Generate Certificate and Verification PDF
                    var template = await _context.CertificateTemplates
                        .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
                    if (template == null && request.ServiceId == 1)
                    {
                        template = await _context.CertificateTemplates
                            .FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
                    }
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
                    }
                    if (template == null)
                    {
                        throw new Exception("Active template not found.");
                    }

                    var digital = await _context.GNOfficerDigitalDetails
                        .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
                    var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
                    var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

                    if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
                    {
                        throw new Exception("GN Officer signature file is missing.");
                    }
                    if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
                    {
                        throw new Exception("GN Officer rubber stamp file is missing.");
                    }

                    var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
                    var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
                    var officerName = officer?.FullName ?? "GN Officer";

                    var verificationToken = Guid.NewGuid().ToString("N");
                    var tokenHash = HashToken(verificationToken);
                    var verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";

                    var editableValues = new CertificateEditableValuesDto
                    {
                        IssuedDate = DateTime.UtcNow.ToString("MMMM dd, yyyy")
                    };

                    var pdfBytes = RenderCertificateToPdf(
                        template.TemplateContent,
                        divisionName,
                        officerName,
                        editableValues,
                        request.RequestNumber,
                        sigPath,
                        stampPath,
                        verificationUrl
                    );

                    string fileHash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(pdfBytes);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }

                    var finalFileName = $"family_card_{newFamilyCardNumber}.pdf";
                    var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                    savedFilePath = Path.Combine(uploadFolder, finalFileName);

                    await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);

                    var certificateAttachment = await _context.UnifiedRequestAttachments
                        .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "GENERATED_CERTIFICATE");

                    if (certificateAttachment != null)
                    {
                        var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), certificateAttachment.FileUrl.TrimStart('/'));
                        if (oldPhysicalPath != savedFilePath && System.IO.File.Exists(oldPhysicalPath))
                        {
                            try { System.IO.File.Delete(oldPhysicalPath); } catch {}
                        }

                        certificateAttachment.FileName = finalFileName;
                        certificateAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                        certificateAttachment.UploadedAt = DateTime.UtcNow;
                        certificateAttachment.FileSize = pdfBytes.Length;
                        certificateAttachment.UploadedBy = gnNic;
                        _context.UnifiedRequestAttachments.Update(certificateAttachment);
                    }
                    else
                    {
                        certificateAttachment = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = finalFileName,
                            FileUrl = $"/uploads/requests/{finalFileName}",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "GENERATED_CERTIFICATE",
                            DisplayName = "Generated Certificate",
                            IsPrimary = true,
                            UploadedBy = gnNic,
                            FileSize = pdfBytes.Length,
                            MimeType = "application/pdf",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(certificateAttachment);
                    }
                    await _context.SaveChangesAsync();

                    var docVerification = await _context.DocumentVerifications
                        .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                    int mainAttachmentId = 0;
                    var mainAtt = await _context.UnifiedRequestAttachments.FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole != "GENERATED_CERTIFICATE");
                    if (mainAtt == null)
                    {
                        mainAtt = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = "application_form.pdf",
                            FileUrl = "/uploads/requests/dummy.pdf",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "SUPPORTING_DOCUMENT",
                            DisplayName = "Application Form",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(mainAtt);
                        await _context.SaveChangesAsync();
                    }
                    mainAttachmentId = mainAtt.AttachmentId;

                    if (docVerification == null)
                    {
                        docVerification = new DocumentVerification
                        {
                            RequestId = request.RequestId,
                            MainAttachmentId = mainAttachmentId,
                            AssignedGNOfficerNIC = gnNic,
                            VerificationStatus = "PUBLISHED",
                            VerificationRemarks = dto.Remarks ?? "Family Card released.",
                            VerifiedDate = DateTime.UtcNow,
                            FinalVerifiedAttachmentId = certificateAttachment.AttachmentId,
                            VerificationTokenHash = tokenHash,
                            VerificationUrl = verificationUrl,
                            OriginalFileHash = template.TemplateId.ToString(),
                            FinalFileHash = fileHash,
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow
                        };
                        _context.DocumentVerifications.Add(docVerification);
                    }
                    else
                    {
                        docVerification.MainAttachmentId = mainAttachmentId;
                        docVerification.AssignedGNOfficerNIC = gnNic;
                        docVerification.VerificationStatus = "PUBLISHED";
                        docVerification.VerificationRemarks = dto.Remarks ?? "Family Card released.";
                        docVerification.VerifiedDate = GetSriLankanTime();
                        docVerification.FinalVerifiedAttachmentId = certificateAttachment.AttachmentId;
                        docVerification.VerificationTokenHash = tokenHash;
                        docVerification.VerificationUrl = verificationUrl;
                        docVerification.OriginalFileHash = template.TemplateId.ToString();
                        docVerification.FinalFileHash = fileHash;
                        docVerification.UpdatedDate = GetSriLankanTime();
                        _context.DocumentVerifications.Update(docVerification);
                    }

                    request.CurrentStatus = "Accepted";
                    request.ApprovedDate = GetSriLankanTime();
                    request.LastUpdated = GetSriLankanTime();
                    _context.UnifiedRequests.Update(request);

                    var latestHistory = await _context.RequestStatusHistories
                        .Where(h => h.RequestId == request.RequestId)
                        .OrderByDescending(h => h.ChangedDate)
                        .FirstOrDefaultAsync();

                    if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                    {
                        AddStatusHistory(request.RequestId, "Accepted", dto.Remarks ?? "Family Card requested and processed successfully.", gnNic);
                    }

                    await SendReleaseNotification(request, gnNic);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Ok(new { success = true, message = "Family Card created and marriage processed successfully." });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                    {
                        try { System.IO.File.Delete(savedFilePath); } catch {}
                    }
                    var detailedError = ex.InnerException != null ? $"{ex.Message} (Inner: {ex.InnerException.Message})" : ex.Message;
                    return StatusCode(500, new { success = false, message = "Failed to process marriage family card request.", error = detailedError });
                }
            }
            else if (request.CategoryId == 53)
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                string? savedFilePath = null;
                try
                {
                    if (request.CurrentStatus == "Accepted")
                    {
                        return BadRequest(new { success = false, message = "The request has already been accepted." });
                    }

                    var familyCardNumber = request.Citizen?.FamilyCardNumber;
                    if (string.IsNullOrEmpty(familyCardNumber))
                    {
                        return BadRequest(new { success = false, message = "Applicant does not belong to a family card." });
                    }

                    var familyCard = await _context.FamilyCards
                        .FirstOrDefaultAsync(fc => fc.FamilyCardNumber == familyCardNumber);

                    if (familyCard == null || familyCard.Status != "Active")
                    {
                        return BadRequest(new { success = false, message = "The selected family card is invalid or inactive." });
                    }

                    officer = await _context.GnOfficers
                        .FirstOrDefaultAsync(o => o.Nic == gnNic);

                    if (officer == null || familyCard.GnDivisionId != officer.GnDivisionId)
                    {
                        return BadRequest(new { success = false, message = "The family card does not belong to the GN Officer's assigned division." });
                    }

                    var reqData = await _context.RequestDataEntries
                        .FirstOrDefaultAsync(rd => rd.RequestId == request.RequestId);

                    if (reqData == null || string.IsNullOrEmpty(reqData.JsonData))
                    {
                        return BadRequest(new { success = false, message = "Request data is missing." });
                    }

                    string newAddress = SafeGetJsonProperty(reqData.JsonData, "newAddress");
                    string newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "newHouseNumber");

                    if (string.IsNullOrEmpty(newAddress))
                    {
                        return BadRequest(new { success = false, message = "New address is missing in the request." });
                    }

                    // Update Family Card
                    familyCard.Address = newAddress;
                    familyCard.HouseNumber = !string.IsNullOrEmpty(newHouseNumber) ? newHouseNumber : null;
                    _context.FamilyCards.Update(familyCard);

                    // Generate Certificate and PDF using Category 1 template or Category 53 template if active
                    var template = await _context.CertificateTemplates
                        .FirstOrDefaultAsync(t => t.CategoryId == request.CategoryId && t.IsActive);
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates
                            .FirstOrDefaultAsync(t => t.CategoryId == 1 && t.IsActive);
                    }
                    if (template == null)
                    {
                        template = await _context.CertificateTemplates.FirstOrDefaultAsync(t => t.IsActive);
                    }
                    if (template == null)
                    {
                        throw new Exception("Active template not found.");
                    }

                    var digital = await _context.GNOfficerDigitalDetails
                        .FirstOrDefaultAsync(d => d.GNOfficerNIC == gnNic);
                    var sigPath = GetPhysicalPath(digital?.SignaturePath, "gn-signatures");
                    var stampPath = GetPhysicalPath(digital?.RubberStampPath, "gn-stamps");

                    if (string.IsNullOrEmpty(sigPath) || !System.IO.File.Exists(sigPath))
                    {
                        throw new Exception("GN Officer signature file is missing.");
                    }
                    if (string.IsNullOrEmpty(stampPath) || !System.IO.File.Exists(stampPath))
                    {
                        throw new Exception("GN Officer rubber stamp file is missing.");
                    }

                    var division = await _context.GnDivisions.FirstOrDefaultAsync(d => d.Id == request.GnDivision);
                    var divisionName = division != null ? division.Name : (request.GnDivision ?? "Unknown");
                    var officerName = officer?.FullName ?? "GN Officer";

                    var verificationToken = Guid.NewGuid().ToString("N");
                    var tokenHash = HashToken(verificationToken);
                    var verificationUrl = $"http://localhost:4200/verify-document/{verificationToken}";

                    var editableValues = new CertificateEditableValuesDto
                    {
                        IssuedDate = DateTime.UtcNow.ToString("MMMM dd, yyyy")
                    };

                    var pdfBytes = RenderCertificateToPdf(
                        template.TemplateContent,
                        divisionName,
                        officerName,
                        editableValues,
                        request.RequestNumber,
                        sigPath,
                        stampPath,
                        verificationUrl
                    );

                    string fileHash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(pdfBytes);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }

                    var finalFileName = $"family_card_{familyCardNumber}.pdf";
                    var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                    savedFilePath = Path.Combine(uploadFolder, finalFileName);

                    await System.IO.File.WriteAllBytesAsync(savedFilePath, pdfBytes);

                    var certificateAttachment = await _context.UnifiedRequestAttachments
                        .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole == "GENERATED_CERTIFICATE");

                    if (certificateAttachment != null)
                    {
                        var oldPhysicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), certificateAttachment.FileUrl.TrimStart('/'));
                        if (oldPhysicalPath != savedFilePath && System.IO.File.Exists(oldPhysicalPath))
                        {
                            try { System.IO.File.Delete(oldPhysicalPath); } catch {}
                        }

                        certificateAttachment.FileName = finalFileName;
                        certificateAttachment.FileUrl = $"/uploads/requests/{finalFileName}";
                        certificateAttachment.UploadedAt = DateTime.UtcNow;
                        certificateAttachment.FileSize = pdfBytes.Length;
                        certificateAttachment.UploadedBy = gnNic;
                        _context.UnifiedRequestAttachments.Update(certificateAttachment);
                    }
                    else
                    {
                        certificateAttachment = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = finalFileName,
                            FileUrl = $"/uploads/requests/{finalFileName}",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "GENERATED_CERTIFICATE",
                            DisplayName = "Generated Certificate",
                            IsPrimary = true,
                            UploadedBy = gnNic,
                            FileSize = pdfBytes.Length,
                            MimeType = "application/pdf",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(certificateAttachment);
                    }
                    await _context.SaveChangesAsync();

                    var docVerification = await _context.DocumentVerifications
                        .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);

                    int mainAttachmentId = 0;
                    var mainAtt = await _context.UnifiedRequestAttachments.FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.DocumentRole != "GENERATED_CERTIFICATE");
                    if (mainAtt == null)
                    {
                        mainAtt = new UnifiedRequestAttachment
                        {
                            RequestId = request.RequestId,
                            FileName = "application_form.pdf",
                            FileUrl = "/uploads/requests/dummy.pdf",
                            FileType = "PDF",
                            UploadedAt = DateTime.UtcNow,
                            DocumentRole = "SUPPORTING_DOCUMENT",
                            DisplayName = "Application Form",
                            IsActive = true
                        };
                        _context.UnifiedRequestAttachments.Add(mainAtt);
                        await _context.SaveChangesAsync();
                    }
                    mainAttachmentId = mainAtt.AttachmentId;

                    if (docVerification == null)
                    {
                        docVerification = new DocumentVerification
                        {
                            RequestId = request.RequestId,
                            MainAttachmentId = mainAttachmentId,
                            AssignedGNOfficerNIC = gnNic,
                            VerificationStatus = "PUBLISHED",
                            VerificationRemarks = dto.Remarks ?? "Family Card Address Change approved.",
                            VerifiedDate = DateTime.UtcNow,
                            FinalVerifiedAttachmentId = certificateAttachment.AttachmentId,
                            VerificationTokenHash = tokenHash,
                            VerificationUrl = verificationUrl,
                            OriginalFileHash = template.TemplateId.ToString(),
                            FinalFileHash = fileHash,
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow
                        };
                        _context.DocumentVerifications.Add(docVerification);
                    }
                    else
                    {
                        docVerification.MainAttachmentId = mainAttachmentId;
                        docVerification.AssignedGNOfficerNIC = gnNic;
                        docVerification.VerificationStatus = "PUBLISHED";
                        docVerification.VerificationRemarks = dto.Remarks ?? "Family Card Address Change approved.";
                        docVerification.VerifiedDate = GetSriLankanTime();
                        docVerification.FinalVerifiedAttachmentId = certificateAttachment.AttachmentId;
                        docVerification.VerificationTokenHash = tokenHash;
                        docVerification.VerificationUrl = verificationUrl;
                        docVerification.OriginalFileHash = template.TemplateId.ToString();
                        docVerification.FinalFileHash = fileHash;
                        docVerification.UpdatedDate = GetSriLankanTime();
                        _context.DocumentVerifications.Update(docVerification);
                    }

                    request.CurrentStatus = "Accepted";
                    request.ApprovedDate = GetSriLankanTime();
                    request.LastUpdated = GetSriLankanTime();
                    _context.UnifiedRequests.Update(request);

                    var latestHistory = await _context.RequestStatusHistories
                        .Where(h => h.RequestId == request.RequestId)
                        .OrderByDescending(h => h.ChangedDate)
                        .FirstOrDefaultAsync();

                    if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                    {
                        AddStatusHistory(request.RequestId, "Accepted", dto.Remarks ?? "Family Card Address Change approved successfully.", gnNic);
                    }

                    await SendReleaseNotification(request, gnNic);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Ok(new { success = true, message = "Family Card Address updated successfully." });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                    {
                        try { System.IO.File.Delete(savedFilePath); } catch {}
                    }
                    var detailedError = ex.InnerException != null ? $"{ex.Message} (Inner: {ex.InnerException.Message})" : ex.Message;
                    return StatusCode(500, new { success = false, message = "Failed to process Family Card Address Change request.", error = detailedError });
                }
            }
            var verification = await _context.DocumentVerifications
                .FirstOrDefaultAsync(v => v.RequestId == request.RequestId);
            if (verification == null || verification.FinalVerifiedAttachmentId == null)
            {
                return BadRequest("No generated certificate found to release.");
            }

            officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
            if (officer == null)
            {
                return BadRequest("Authenticated GN Officer record not found.");
            }

            if (verification.AssignedGNOfficerNIC != gnNic)
            {
                return Forbid("Cannot publish a document generated by a different GN Officer.");
            }

            if (request.AssignedGnOfficerNic != gnNic)
            {
                return Forbid("Cannot publish a document for a request assigned to another officer.");
            }

            if (request.GnDivision != officer.GnDivisionId)
            {
                return BadRequest("Request GN division does not match the officer's division.");
            }

            if (request.CurrentStatus != "Verified" && request.CurrentStatus != "VERIFIED")
            {
                return BadRequest("The request status must be Verified to publish.");
            }

            if (verification.VerificationStatus != "GENERATED")
            {
                if (verification.VerificationStatus == "PUBLISHED")
                {
                    return BadRequest("This certificate has already been released.");
                }
                return BadRequest("The document verification status must be GENERATED to publish.");
            }

            var generatedAtt = await _context.UnifiedRequestAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == verification.FinalVerifiedAttachmentId);
            if (generatedAtt == null || !generatedAtt.IsActive || generatedAtt.DocumentRole == "SUPERSEDED_CERTIFICATE")
            {
                return BadRequest("The generated document has been superseded or invalidated. Please regenerate.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update verification status to PUBLISHED
                verification.VerificationStatus = "PUBLISHED";
                verification.VerificationRemarks = dto.Remarks;
                verification.UpdatedDate = GetSriLankanTime();
                _context.DocumentVerifications.Update(verification);

                // Update request status to Accepted
                request.CurrentStatus = "Accepted";
                request.ApprovedDate = GetSriLankanTime();
                request.LastUpdated = GetSriLankanTime();
                _context.UnifiedRequests.Update(request);

                // Add status history record
                var latestHistory = await _context.RequestStatusHistories
                    .Where(h => h.RequestId == request.RequestId)
                    .OrderByDescending(h => h.ChangedDate)
                    .FirstOrDefaultAsync();

                if (latestHistory == null || latestHistory.Status.ToUpper() != "ACCEPTED")
                {
                    AddStatusHistory(request.RequestId, "Accepted", dto.Remarks, gnNic);
                }

                // Update Welfare Status for Category ID 44
                if (request.CategoryId == 44)
                {
                    await UpdateCategory44WelfareStatusAsync(request, gnNic);
                }

                await SendReleaseNotification(request, gnNic);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Certificate released and published successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Failed to release certificate.", error = ex.Message });
            }
        }

        private byte[] RenderCertificateToPdf(
            string templateContent,
            string gnDivisionName,
            string gnOfficerName,
            CertificateEditableValuesDto editable,
            string requestNumber,
            string sigPath,
            string stampPath,
            string qrUrl)
        {
            // 1. Resolve CategoryId & GnDivisionId
            int categoryId = 0;
            string divisionId = string.Empty;
            try
            {
                var req = _context.UnifiedRequests.FirstOrDefault(r => r.RequestNumber == requestNumber);
                if (req != null)
                {
                    categoryId = req.CategoryId;
                    divisionId = req.GnDivision ?? string.Empty;
                }
            }
            catch {}

            string dsDivisionName = "Unknown";
            string districtName = "Unknown";
            if (!string.IsNullOrEmpty(divisionId))
            {
                try
                {
                    var div = _context.GnDivisions.FirstOrDefault(d => d.Id == divisionId);
                    if (div != null)
                    {
                        var dsDiv = _context.DsDivisions
                            .Include(d => d.District)
                            .FirstOrDefault(d => d.Id == div.DsDivisionId);
                        if (dsDiv != null)
                        {
                            dsDivisionName = dsDiv.Name;
                            if (dsDiv.District != null)
                            {
                                districtName = dsDiv.District.Name;
                            }
                        }
                    }
                }
                catch {}
            }


            if ((categoryId >= 1 && categoryId <= 6) || categoryId == 53)
            {
                var req = _context.UnifiedRequests
                    .Include(r => r.Citizen)
                    .FirstOrDefault(r => r.RequestNumber == requestNumber);
                
                string familyCardNumber = req?.Citizen?.FamilyCardNumber ?? string.Empty;
                var familyCard = _context.FamilyCards
                    .Include(fc => fc.HeadOfHousehold)
                    .FirstOrDefault(fc => fc.FamilyCardNumber == familyCardNumber);
                
                string headOfHouseholdNic = familyCard?.HeadOfHouseholdNic ?? string.Empty;
                string headOfHouseholdName = "Unknown";
                if (!string.IsNullOrEmpty(headOfHouseholdNic))
                {
                    var headCit = _context.CitizenMasters.FirstOrDefault(c => c.Nic == headOfHouseholdNic);
                    if (headCit != null)
                    {
                        headOfHouseholdName = headCit.FullName;
                    }
                }
                string houseNumber = familyCard?.HouseNumber ?? string.Empty;
                string address = familyCard?.Address ?? string.Empty;
                string registeredDate = familyCard?.RegisteredDate.ToString("yyyy-MM-dd") ?? string.Empty;
                string familyCardStatus = familyCard?.Status ?? "Active";

                var reqData = _context.RequestDataEntries.FirstOrDefault(rd => rd.RequestId == (req != null ? req.RequestId : 0));
                string json = reqData?.JsonData ?? string.Empty;

                string requestType = SafeGetJsonProperty(json, "requestType");
                if (string.IsNullOrEmpty(requestType))
                {
                    requestType = "Marriage";
                }

                bool isCategory5NotAccepted = categoryId == 5 && 
                                              req != null && 
                                              req.CurrentStatus != "Accepted" && 
                                              req.CurrentStatus != "ACCEPTED";

                CitizenMaster? cat5HeadCitizen = null;
                CitizenMaster? cat5NonHeadCitizen = null;
                string cat5NonHeadRelationship = "Spouse";
                List<CitizenMaster> cat5MovingCitizens = new List<CitizenMaster>();

                if (isCategory5NotAccepted && req != null)
                {
                    if (string.Equals(requestType, "Marriage", StringComparison.OrdinalIgnoreCase))
                    {
                        var applicant = req.Citizen;
                        string spouseNic = SafeGetJsonProperty(json, "spouseNic");
                        var spouse = _context.CitizenMasters.FirstOrDefault(c => c.Nic == spouseNic);

                        if (applicant != null && spouse != null)
                        {
                            string sameGnDivisionVal = SafeGetJsonProperty(json, "sameGnDivision");
                            bool isSameGn = string.Equals(sameGnDivisionVal, "Yes", StringComparison.OrdinalIgnoreCase) || 
                                            string.Equals(sameGnDivisionVal, "Y", StringComparison.OrdinalIgnoreCase);

                            string destGnDivisionId;
                            if (isSameGn)
                            {
                                destGnDivisionId = applicant.GnDivisionId;
                            }
                            else
                            {
                                destGnDivisionId = SafeGetJsonProperty(json, "gnDivisionId");
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = SafeGetJsonProperty(json, "newGnDivision");
                                }
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = SafeGetJsonProperty(json, "gnDivision");
                                }
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = req.GnDivision ?? string.Empty;
                                }
                            }

                            // Generate unique family card number
                            var digits = new string((destGnDivisionId ?? string.Empty).Where(char.IsDigit).ToArray());
                            if (string.IsNullOrEmpty(digits))
                            {
                                digits = "10";
                            }
                            
                            string newFamilyCardNumber = string.Empty;
                            int seq = 1;
                            while (true)
                            {
                                string testCardNumber = $"FC-{digits}{seq:D2}";
                                bool exists = _context.FamilyCards.Any(fc => fc.FamilyCardNumber == testCardNumber)
                                              || string.Equals(testCardNumber, applicant.FamilyCardNumber, StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(testCardNumber, spouse.FamilyCardNumber, StringComparison.OrdinalIgnoreCase);
                                
                                if (!exists)
                                {
                                    newFamilyCardNumber = testCardNumber;
                                    break;
                                }
                                seq++;
                            }

                            familyCardNumber = newFamilyCardNumber;

                            // Decide Head based on gender
                            string newHeadOfHouseholdNic = SafeGetJsonProperty(json, "newHeadOfHousehold");
                            bool applicantIsMale = string.Equals(applicant.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                            bool spouseIsMale = string.Equals(spouse.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                            bool applicantIsFemale = string.Equals(applicant.Gender, "Female", StringComparison.OrdinalIgnoreCase);
                            bool spouseIsFemale = string.Equals(spouse.Gender, "Female", StringComparison.OrdinalIgnoreCase);

                            if (applicantIsFemale && spouseIsMale && string.Equals(newHeadOfHouseholdNic, applicant.Nic))
                            {
                                cat5HeadCitizen = applicant;
                                cat5NonHeadCitizen = spouse;
                                cat5NonHeadRelationship = "Husband";
                            }
                            else if (spouseIsFemale && applicantIsMale && string.Equals(newHeadOfHouseholdNic, spouse.Nic))
                            {
                                cat5HeadCitizen = spouse;
                                cat5NonHeadCitizen = applicant;
                                cat5NonHeadRelationship = "Husband";
                            }
                            else if (applicantIsMale)
                            {
                                cat5HeadCitizen = applicant;
                                cat5NonHeadCitizen = spouse;
                                cat5NonHeadRelationship = spouseIsFemale ? "Wife" : "Spouse";
                            }
                            else if (spouseIsMale)
                            {
                                cat5HeadCitizen = spouse;
                                cat5NonHeadCitizen = applicant;
                                cat5NonHeadRelationship = applicantIsFemale ? "Wife" : "Spouse";
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(newHeadOfHouseholdNic) && string.Equals(newHeadOfHouseholdNic, spouse.Nic))
                                {
                                    cat5HeadCitizen = spouse;
                                    cat5NonHeadCitizen = applicant;
                                }
                                else
                                {
                                    cat5HeadCitizen = applicant;
                                    cat5NonHeadCitizen = spouse;
                                }
                                cat5NonHeadRelationship = "Spouse";
                            }

                            headOfHouseholdNic = cat5HeadCitizen.Nic;
                            headOfHouseholdName = cat5HeadCitizen.FullName;

                            var newAddress = SafeGetJsonProperty(json, "newAddress");
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = SafeGetJsonProperty(json, "address");
                            }
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = "No Address Provided";
                            }

                            var newHouseNumber = SafeGetJsonProperty(json, "houseNumber");
                            if (string.IsNullOrEmpty(newHouseNumber))
                            {
                                newHouseNumber = SafeGetJsonProperty(json, "newHouseNumber");
                            }

                            houseNumber = newHouseNumber ?? string.Empty;
                            address = newAddress;
                            registeredDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            familyCardStatus = "Active";
                        }
                    }
                    else if (string.Equals(requestType, "DivorceOrSeparation", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(requestType, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        var memberNicsToMove = SafeGetJsonArray(json, "familyMembersToMove");
                        var movingCitizens = _context.CitizenMasters
                            .Where(c => memberNicsToMove.Contains(c.Nic))
                            .ToList();

                        if (movingCitizens.Any())
                        {
                            var applicant = req.Citizen;
                            string destGnDivisionId = string.Empty;
                            string sameGnDivisionVal = SafeGetJsonProperty(json, "sameGnDivision");
                            bool isSameGn = string.Equals(sameGnDivisionVal, "Yes", StringComparison.OrdinalIgnoreCase) || 
                                            string.Equals(sameGnDivisionVal, "Y", StringComparison.OrdinalIgnoreCase);

                            if (isSameGn && applicant != null)
                            {
                                destGnDivisionId = applicant.GnDivisionId;
                            }
                            else
                            {
                                destGnDivisionId = SafeGetJsonProperty(json, "gnDivisionId");
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = SafeGetJsonProperty(json, "newGnDivision");
                                }
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = SafeGetJsonProperty(json, "gnDivision");
                                }
                                if (string.IsNullOrEmpty(destGnDivisionId))
                                {
                                    destGnDivisionId = req.GnDivision ?? string.Empty;
                                }
                            }

                            // Generate unique family card number
                            var digits = new string((destGnDivisionId ?? string.Empty).Where(char.IsDigit).ToArray());
                            if (string.IsNullOrEmpty(digits))
                            {
                                digits = "10";
                            }
                            
                            string newFamilyCardNumber = string.Empty;
                            int seq = 1;
                            while (true)
                            {
                                string testCardNumber = $"FC-{digits}{seq:D2}";
                                bool exists = _context.FamilyCards.Any(fc => fc.FamilyCardNumber == testCardNumber)
                                              || movingCitizens.Any(c => string.Equals(testCardNumber, c.FamilyCardNumber, StringComparison.OrdinalIgnoreCase));
                                
                                if (!exists)
                                {
                                    newFamilyCardNumber = testCardNumber;
                                    break;
                                }
                                seq++;
                            }

                            familyCardNumber = newFamilyCardNumber;

                            string newHeadOfHouseholdNic = SafeGetJsonProperty(json, "newHeadOfHousehold");
                            cat5HeadCitizen = movingCitizens.FirstOrDefault(c => c.Nic == newHeadOfHouseholdNic) ?? movingCitizens[0];

                            headOfHouseholdNic = cat5HeadCitizen.Nic;
                            headOfHouseholdName = cat5HeadCitizen.FullName;
                            cat5MovingCitizens = movingCitizens;

                            var newAddress = SafeGetJsonProperty(json, "newAddress");
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = SafeGetJsonProperty(json, "address");
                            }
                            if (string.IsNullOrEmpty(newAddress))
                            {
                                newAddress = "No Address Provided";
                            }

                            var newHouseNumber = SafeGetJsonProperty(json, "houseNumber");
                            if (string.IsNullOrEmpty(newHouseNumber))
                            {
                                newHouseNumber = SafeGetJsonProperty(json, "newHouseNumber");
                            }

                            houseNumber = newHouseNumber ?? string.Empty;
                            address = newAddress;
                            registeredDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            familyCardStatus = "Active";
                        }
                    }
                }

                if (categoryId == 4 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    string newHeadNic = SafeGetJsonProperty(reqData.JsonData, "nic");
                    if (!string.IsNullOrEmpty(newHeadNic))
                    {
                        var newHeadCit = _context.CitizenMasters.FirstOrDefault(c => c.Nic == newHeadNic);
                        if (newHeadCit != null)
                        {
                            headOfHouseholdName = newHeadCit.FullName;
                            headOfHouseholdNic = newHeadNic;
                        }
                    }
                }
                else if (categoryId == 3 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    string newHeadNic = SafeGetJsonProperty(reqData.JsonData, "newFamilyHead");
                    if (!string.IsNullOrEmpty(newHeadNic))
                    {
                        var newHeadCit = _context.CitizenMasters.FirstOrDefault(c => c.Nic == newHeadNic);
                        if (newHeadCit == null)
                        {
                            newHeadCit = _context.CitizenMasters.FirstOrDefault(c => c.FamilyCardNumber == familyCardNumber && c.FullName == newHeadNic);
                        }
                        if (newHeadCit != null)
                        {
                            headOfHouseholdName = newHeadCit.FullName;
                            headOfHouseholdNic = newHeadCit.Nic;
                        }
                    }
                }

                // Category 42: Register to New GN Division (simulating new address details for preview)
                bool isCategory42NotAccepted = categoryId == 42 &&
                                               req != null &&
                                               req.CurrentStatus != "Accepted" &&
                                               req.CurrentStatus != "ACCEPTED";

                if (isCategory42NotAccepted && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    var newAddress = SafeGetJsonProperty(reqData.JsonData, "newResidentialAddress");
                    if (string.IsNullOrEmpty(newAddress))
                    {
                        newAddress = SafeGetJsonProperty(reqData.JsonData, "newAddress");
                    }
                    if (!string.IsNullOrEmpty(newAddress))
                    {
                        address = newAddress;
                    }

                    var newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "newHouseNumber");
                    if (!string.IsNullOrEmpty(newHouseNumber))
                    {
                        houseNumber = newHouseNumber;
                    }
                }

                // Category 53: Family Card Address Change (simulating new address details for preview)
                bool isCategory53NotAccepted = categoryId == 53 &&
                                               req != null &&
                                               req.CurrentStatus != "Accepted" &&
                                               req.CurrentStatus != "ACCEPTED";

                if (isCategory53NotAccepted && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    var newAddress = SafeGetJsonProperty(reqData.JsonData, "newAddress");
                    if (!string.IsNullOrEmpty(newAddress))
                    {
                        address = newAddress;
                    }

                    var newHouseNumber = SafeGetJsonProperty(reqData.JsonData, "newHouseNumber");
                    if (!string.IsNullOrEmpty(newHouseNumber))
                    {
                        houseNumber = newHouseNumber;
                    }
                }

                var membersList = new List<FamilyMemberRowDto>();

                var head = _context.CitizenMasters
                    .FirstOrDefault(c => c.Nic == headOfHouseholdNic);
                if (head != null && head.LifeStatus == "Alive")
                {
                    membersList.Add(new FamilyMemberRowDto
                    {
                        Nic = head.Nic,
                        FullName = head.FullName,
                        Relationship = "Head",
                        Gender = head.Gender,
                        Dob = head.Dob.ToString("yyyy-MM-dd"),
                        Occupation = head.Occupation ?? "Not Applicable",
                        MonthlyIncome = head.MonthlyIncome ?? 0
                    });
                }

                if (isCategory5NotAccepted)
                {
                    if (string.Equals(requestType, "Marriage", StringComparison.OrdinalIgnoreCase))
                    {
                        if (cat5NonHeadCitizen != null)
                        {
                            membersList.Add(new FamilyMemberRowDto
                            {
                                Nic = cat5NonHeadCitizen.Nic,
                                FullName = cat5NonHeadCitizen.FullName,
                                Relationship = cat5NonHeadRelationship,
                                Gender = cat5NonHeadCitizen.Gender,
                                Dob = cat5NonHeadCitizen.Dob.ToString("yyyy-MM-dd"),
                                Occupation = cat5NonHeadCitizen.Occupation ?? "Not Applicable",
                                MonthlyIncome = cat5NonHeadCitizen.MonthlyIncome ?? 0
                            });
                        }
                    }
                    else if (string.Equals(requestType, "DivorceOrSeparation", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(requestType, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var m in cat5MovingCitizens)
                        {
                            if (m.Nic != headOfHouseholdNic)
                            {
                                string rel = "Member";
                                if (m.RelationshipToHead != "Head" && !string.IsNullOrEmpty(m.RelationshipToHead))
                                {
                                    rel = m.RelationshipToHead;
                                }

                                membersList.Add(new FamilyMemberRowDto
                                {
                                    Nic = m.Nic,
                                    FullName = m.FullName,
                                    Relationship = rel,
                                    Gender = m.Gender,
                                    Dob = m.Dob.ToString("yyyy-MM-dd"),
                                    Occupation = m.Occupation ?? "Not Applicable",
                                    MonthlyIncome = m.MonthlyIncome ?? 0
                                });
                            }
                        }
                    }
                }
                else
                {
                    var otherMembers = _context.CitizenMasters
                        .Where(c => c.FamilyCardNumber == familyCardNumber && c.Nic != headOfHouseholdNic && c.LifeStatus == "Alive")
                        .ToList();

                    foreach (var m in otherMembers)
                    {
                        membersList.Add(new FamilyMemberRowDto
                        {
                            Nic = m.Nic,
                            FullName = m.FullName,
                            Relationship = m.RelationshipToHead ?? "Member",
                            Gender = m.Gender,
                            Dob = m.Dob.ToString("yyyy-MM-dd"),
                            Occupation = m.Occupation ?? "Not Applicable",
                            MonthlyIncome = m.MonthlyIncome ?? 0
                        });
                    }
                }

                string? childFullName = null;
                string? childGender = null;
                string? childDob = null;
                string? childRelationship = null;
                if (reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(reqData.JsonData);
                        if (doc.RootElement.TryGetProperty("childFullName", out var p1)) childFullName = p1.GetString();
                        if (doc.RootElement.TryGetProperty("gender", out var p2)) childGender = p2.GetString();
                        if (doc.RootElement.TryGetProperty("dateOfBirth", out var p3)) childDob = p3.GetString();
                        if (doc.RootElement.TryGetProperty("relationshipOfChild", out var p4)) childRelationship = p4.GetString();
                    }
                    catch {}
                }

                if (categoryId == 1 && !string.IsNullOrEmpty(childFullName) && !string.IsNullOrEmpty(childDob))
                {
                    DateOnly childDobDate = DateOnly.Parse(childDob);
                    bool newbornAlreadyInDb = membersList.Any(m => m.FullName == childFullName && m.Dob == childDob);
                    if (!newbornAlreadyInDb && req?.CurrentStatus != "Accepted")
                    {
                        var prefix = $"T{childDobDate:yyyyMMdd}";
                        var existingNics = _context.CitizenMasters.Where(c => c.Nic.StartsWith(prefix)).Select(c => c.Nic).ToList();
                        string tempNic = "";
                        for (int i = 901; i <= 999; i++)
                        {
                            var testNic = $"{prefix}{i:D3}";
                            if (!existingNics.Contains(testNic)) { tempNic = testNic; break; }
                        }
                        if (string.IsNullOrEmpty(tempNic))
                        {
                            for (int i = 1; i <= 900; i++)
                            {
                                var testNic = $"{prefix}{i:D3}";
                                if (!existingNics.Contains(testNic)) { tempNic = testNic; break; }
                            }
                        }

                        membersList.Add(new FamilyMemberRowDto
                        {
                            Nic = tempNic,
                            FullName = childFullName,
                            Relationship = childRelationship ?? "Son/Daughter",
                            Gender = childGender ?? "Unknown",
                            Dob = childDob,
                            Occupation = "Not Applicable",
                            MonthlyIncome = 0
                        });
                    }
                }
                else if (categoryId == 2 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    string transferredNic = SafeGetJsonProperty(reqData.JsonData, "nic");
                    string relationshipToHeadVal = SafeGetJsonProperty(reqData.JsonData, "relationshipToFamilyHead");
                    if (string.IsNullOrEmpty(relationshipToHeadVal))
                    {
                        relationshipToHeadVal = "Member";
                    }

                    if (!string.IsNullOrEmpty(transferredNic) && !membersList.Any(m => m.Nic == transferredNic) && req?.CurrentStatus != "Accepted")
                    {
                        var transCit = _context.CitizenMasters.FirstOrDefault(c => c.Nic == transferredNic);
                        if (transCit != null && transCit.LifeStatus == "Alive")
                        {
                            membersList.Add(new FamilyMemberRowDto
                            {
                                Nic = transCit.Nic,
                                FullName = transCit.FullName,
                                Relationship = relationshipToHeadVal ?? "Member",
                                Gender = transCit.Gender,
                                Dob = transCit.Dob.ToString("yyyy-MM-dd"),
                                Occupation = transCit.Occupation ?? "Not Applicable",
                                MonthlyIncome = transCit.MonthlyIncome ?? 0
                            });
                        }
                    }
                }
                else if (categoryId == 3 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    string deceasedNic = SafeGetJsonProperty(reqData.JsonData, "nic") != "" ? SafeGetJsonProperty(reqData.JsonData, "nic") : SafeGetJsonProperty(reqData.JsonData, "deceasedNic");
                    if (string.IsNullOrEmpty(deceasedNic))
                    {
                        deceasedNic = SafeGetJsonProperty(reqData.JsonData, "affectedPerson");
                    }
                    if (!string.IsNullOrEmpty(deceasedNic))
                    {
                        membersList.RemoveAll(m => m.Nic == deceasedNic);
                    }

                    // Check if new head is assigned
                    string newHeadNic = SafeGetJsonProperty(reqData.JsonData, "newFamilyHead");
                    if (!string.IsNullOrEmpty(newHeadNic))
                    {
                        var newHeadRow = membersList.FirstOrDefault(m => m.Nic == newHeadNic);
                        if (newHeadRow != null)
                        {
                            membersList.Remove(newHeadRow);
                            newHeadRow.Relationship = "Head";
                            membersList.Insert(0, newHeadRow);
                        }
                    }
                }
                else if (categoryId == 4 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    foreach (var m in membersList)
                    {
                        if (m.Nic != headOfHouseholdNic)
                        {
                            if (m.Relationship == "Head" || m.Relationship == "Self")
                            {
                                m.Relationship = "Member";
                            }
                        }
                    }
                }
                else if (categoryId == 6 && reqData != null && !string.IsNullOrEmpty(reqData.JsonData))
                {
                    string migratedNic = SafeGetJsonProperty(reqData.JsonData, "nic");
                    if (!string.IsNullOrEmpty(migratedNic))
                    {
                        membersList.RemoveAll(m => m.Nic == migratedNic);
                    }
                }

                return RenderFamilyCardToPdf(
                    templateContent,
                    gnDivisionName,
                    gnOfficerName,
                    editable,
                    requestNumber,
                    sigPath,
                    stampPath,
                    qrUrl,
                    familyCardNumber,
                    headOfHouseholdName,
                    headOfHouseholdNic,
                    houseNumber,
                    address,
                    registeredDate,
                    familyCardStatus,
                    membersList,
                    dsDivisionName,
                    districtName
                );
            }

            // Resolve national emblem base64 - check .jpeg first, fall back to .png
            string emblemBase64 = string.Empty;
            string[] emblemCandidates = {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "national-emblem.jpeg"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "national-emblem.jpg"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "national-emblem.png")
            };
            foreach (var candidate in emblemCandidates)
            {
                if (System.IO.File.Exists(candidate))
                {
                    try
                    {
                        byte[] emblemBytes = System.IO.File.ReadAllBytes(candidate);
                        string emblemExt = Path.GetExtension(candidate).ToLowerInvariant();
                        string emblemMime = (emblemExt == ".png") ? "image/png" : "image/jpeg";
                        emblemBase64 = $"data:{emblemMime};base64,{Convert.ToBase64String(emblemBytes)}";
                    }
                    catch {}
                    break;
                }
            }

            // Generate QR code base64
            string qrBase64 = string.Empty;
            try
            {
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                qrBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
            }
            catch {}

            // Load signature and stamp base64
            string sigBase64 = string.Empty;
            if (!string.IsNullOrEmpty(sigPath) && System.IO.File.Exists(sigPath))
            {
                try
                {
                    byte[] sigBytes = System.IO.File.ReadAllBytes(sigPath);
                    string ext = Path.GetExtension(sigPath).ToLowerInvariant();
                    string mime = ext == ".png" ? "image/png" : "image/jpeg";
                    sigBase64 = $"data:{mime};base64,{Convert.ToBase64String(sigBytes)}";
                }
                catch {}
            }

            string stampBase64 = string.Empty;
            if (!string.IsNullOrEmpty(stampPath) && System.IO.File.Exists(stampPath))
            {
                try
                {
                    byte[] stampBytes = System.IO.File.ReadAllBytes(stampPath);
                    string ext = Path.GetExtension(stampPath).ToLowerInvariant();
                    string mime = ext == ".png" ? "image/png" : "image/jpeg";
                    stampBase64 = $"data:{mime};base64,{Convert.ToBase64String(stampBytes)}";
                }
                catch {}
            }

            // Substitute all text and image placeholders in the template HTML
            string interpolated = templateContent;

            // 1. First, replace src="..." patterns to avoid nesting image tags
            interpolated = ReplaceSrcPlaceholder(interpolated, "{{GovernmentEmblem}}", emblemBase64);
            interpolated = ReplaceSrcPlaceholder(interpolated, "{{OfficerSignature}}", sigBase64);
            interpolated = ReplaceSrcPlaceholder(interpolated, "{{RubberStamp}}", stampBase64);
            interpolated = ReplaceSrcPlaceholder(interpolated, "{{QrCode}}", qrBase64);

            // 2. Replace remaining bare placeholders with styled img tags
            string emblemTag = !string.IsNullOrEmpty(emblemBase64)
                ? $"<img src=\"{emblemBase64}\" style=\"max-height:110px;max-width:110px;display:block;margin:0 auto;\" />"
                : string.Empty;
            interpolated = interpolated.Replace("{{GovernmentEmblem}}", emblemTag, StringComparison.OrdinalIgnoreCase);

            string sigTag = !string.IsNullOrEmpty(sigBase64)
                ? $"<img src=\"{sigBase64}\" style=\"max-height:80px;max-width:200px;display:block;margin:0 auto;\" />"
                : string.Empty;
            interpolated = interpolated.Replace("{{OfficerSignature}}", sigTag, StringComparison.OrdinalIgnoreCase);

            string stampTag = !string.IsNullOrEmpty(stampBase64)
                ? $"<img src=\"{stampBase64}\" style=\"max-height:150px;max-width:150px;display:block;margin:0 auto;\" />"
                : string.Empty;
            interpolated = interpolated.Replace("{{RubberStamp}}", stampTag, StringComparison.OrdinalIgnoreCase);

            string qrTag = !string.IsNullOrEmpty(qrBase64)
                ? $"<img src=\"{qrBase64}\" style=\"max-width:100%;max-height:100%;object-fit:contain;display:block;margin:0 auto;\" />"
                : string.Empty;
            interpolated = interpolated.Replace("{{QrCode}}", qrTag, StringComparison.OrdinalIgnoreCase);

            // Replace standard details
            interpolated = interpolated.Replace("{{CertificateName}}", editable.CertificateName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{CertificateTitle}}", editable.CertificateName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{CitizenFullName}}", editable.CitizenFullName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{CitizenNIC}}", editable.CitizenNic ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{CitizenNic}}", editable.CitizenNic ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            string addrClean = editable.ResidentialAddress ?? string.Empty;
            if (addrClean.Contains("\n")) addrClean = addrClean.Replace("\r", "").Replace("\n", ", ");
            interpolated = interpolated.Replace("{{ResidentialAddress}}", addrClean, StringComparison.OrdinalIgnoreCase);

            interpolated = interpolated.Replace("{{ResidenceFromDate}}", editable.ResidenceFromDate ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{ResidencePeriod}}", editable.ResidencePeriod ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{TemporaryAddress}}", editable.TemporaryAddress ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{TemporaryFromDate}}", editable.TemporaryFromDate ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{TemporaryToDate}}", editable.TemporaryToDate ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{TemporaryResidenceReason}}", editable.TemporaryResidenceReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{DeceasedFullName}}", editable.DeceasedFullName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{DeceasedNic}}", editable.DeceasedNic ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{DeceasedAddress}}", editable.DeceasedAddress ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{DateOfDeath}}", editable.DateOfDeath ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{PlaceOfDeath}}", editable.PlaceOfDeath ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{ReasonOfDeath}}", editable.ReasonOfDeath ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{Occupation}}", editable.Occupation ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{CitizenOccupation}}", editable.Occupation ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{MonthlyIncome}}", editable.MonthlyIncome ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{AnnualIncome}}", editable.AnnualIncome ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{RequiredFor}}", editable.RequiredFor ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{RequestNumber}}", requestNumber, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{IssuedDate}}", editable.IssuedDate ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{GnDivisionId}}", divisionId, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{GnDivisionName}}", gnDivisionName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{DsDivisionName}}", dsDivisionName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{DistrictName}}", districtName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{GnOfficerName}}", gnOfficerName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{CertificateBody}}", editable.CertificateBody ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("<!-- Certificate content will be generated here -->", editable.CertificateBody ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            // Also decode HTML entities in the template (e.g. &quot; → ")
            interpolated = interpolated.Replace("&quot;", "\"");

            return RenderHtmlToPdf(interpolated);
        }

        private string ReplaceSrcPlaceholder(string html, string placeholder, string base64Data)
        {
            if (string.IsNullOrEmpty(html)) return html;
            html = html.Replace($"src=\"{placeholder}\"", $"src=\"{base64Data}\"", StringComparison.OrdinalIgnoreCase);
            html = html.Replace($"src='{placeholder}'", $"src='{base64Data}'", StringComparison.OrdinalIgnoreCase);
            html = html.Replace($"src={placeholder}", $"src=\"{base64Data}\"", StringComparison.OrdinalIgnoreCase);
            html = html.Replace($"src = \"{placeholder}\"", $"src=\"{base64Data}\"", StringComparison.OrdinalIgnoreCase);
            html = html.Replace($"src = '{placeholder}'", $"src='{base64Data}'", StringComparison.OrdinalIgnoreCase);
            html = html.Replace($"src = {placeholder}", $"src=\"{base64Data}\"", StringComparison.OrdinalIgnoreCase);
            return html;
        }





        private byte[] RenderFamilyCardToPdf(
            string templateContent,
            string gnDivisionName,
            string gnOfficerName,
            CertificateEditableValuesDto editable,
            string requestNumber,
            string sigPath,
            string stampPath,
            string qrUrl,
            string familyCardNumber,
            string headOfHouseholdName,
            string headOfHouseholdNic,
            string houseNumber,
            string address,
            string registeredDate,
            string familyCardStatus,
            List<FamilyMemberRowDto> members,
            string dsDivisionName,
            string districtName)
        {
            // Resolve GnDivisionId
            string divisionId = string.Empty;
            try
            {
                var req = _context.UnifiedRequests.FirstOrDefault(r => r.RequestNumber == requestNumber);
                if (req != null) divisionId = req.GnDivision ?? string.Empty;
            }
            catch { }

            // Build members HTML rows
            var membersHtmlBuilder = new System.Text.StringBuilder();
            int mIdx = 1;
            decimal totalIncome = 0;
            foreach (var m in members)
            {
                totalIncome += m.MonthlyIncome;
                membersHtmlBuilder.AppendLine("<tr>");
                membersHtmlBuilder.AppendLine($"  <td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{mIdx}</td>");
                membersHtmlBuilder.AppendLine($"  <td style=\"border:1px solid #000; padding:6px 3px; font-size:10px;\">{m.Nic}</td>");
                membersHtmlBuilder.AppendLine($"  <td style=\"border:1px solid #000; padding:6px 3px;\">{m.FullName}</td>");
                membersHtmlBuilder.AppendLine($"  <td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Relationship}</td>");
                membersHtmlBuilder.AppendLine($"  <td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Gender}</td>");
                membersHtmlBuilder.AppendLine($"  <td style=\"border:1px solid #000; padding:6px 3px; text-align:center;\">{m.Dob}</td>");
                membersHtmlBuilder.AppendLine($"  <td style=\"border:1px solid #000; padding:6px 3px;\">{m.Occupation}</td>");
                membersHtmlBuilder.AppendLine($"  <td style=\"border:1px solid #000; padding:6px 3px; text-align:right;\">Rs.&nbsp;{m.MonthlyIncome:N2}</td>");
                membersHtmlBuilder.AppendLine("</tr>");
                mIdx++;
            }

            // Resolve national emblem base64 - check .jpeg first, fall back to .png
            string emblemBase64 = string.Empty;
            string[] emblemCandidates = {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "national-emblem.jpeg"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "national-emblem.jpg"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "national-emblem.png")
            };
            foreach (var candidate in emblemCandidates)
            {
                if (System.IO.File.Exists(candidate))
                {
                    try
                    {
                        byte[] emblemBytes = System.IO.File.ReadAllBytes(candidate);
                        string emblemExt = Path.GetExtension(candidate).ToLowerInvariant();
                        string emblemMime = (emblemExt == ".png") ? "image/png" : "image/jpeg";
                        emblemBase64 = $"data:{emblemMime};base64,{Convert.ToBase64String(emblemBytes)}";
                    }
                    catch {}
                    break;
                }
            }

            string emblemTag = !string.IsNullOrEmpty(emblemBase64)
                ? $"<img src=\"{emblemBase64}\" style=\"max-height:110px;max-width:110px;display:block;margin:0 auto;\" />"
                : string.Empty;

            // Generate QR code as base64 data URI
            string qrBase64Tag = string.Empty;
            try
            {
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                qrBase64Tag = $"<img src=\"data:image/png;base64,{Convert.ToBase64String(qrBytes)}\" style=\"max-width:100%;max-height:100%;object-fit:contain;display:block;margin:0 auto;\" />";
            }
            catch { qrBase64Tag = ""; }

            // Load signature and stamp as base64 data URIs
            string sigTag = string.Empty;
            if (!string.IsNullOrEmpty(sigPath) && System.IO.File.Exists(sigPath))
            {
                try
                {
                    byte[] sigBytes = System.IO.File.ReadAllBytes(sigPath);
                    string ext = Path.GetExtension(sigPath).ToLowerInvariant();
                    string mime = ext == ".png" ? "image/png" : "image/jpeg";
                    sigTag = $"<img src=\"data:{mime};base64,{Convert.ToBase64String(sigBytes)}\" style=\"max-height:85px;max-width:200px;display:block;margin:0 auto;\" />";
                }
                catch { sigTag = ""; }
            }

            string stampTag = string.Empty;
            if (!string.IsNullOrEmpty(stampPath) && System.IO.File.Exists(stampPath))
            {
                try
                {
                    byte[] stampBytes = System.IO.File.ReadAllBytes(stampPath);
                    string ext = Path.GetExtension(stampPath).ToLowerInvariant();
                    string mime = ext == ".png" ? "image/png" : "image/jpeg";
                    stampTag = $"<img src=\"data:{mime};base64,{Convert.ToBase64String(stampBytes)}\" style=\"max-height:120px;max-width:120px;display:block;margin:0 auto;\" />";
                }
                catch { stampTag = ""; }
            }

            // Substitute all placeholders in the template
            string interpolated = templateContent;
            interpolated = ReplaceSrcPlaceholder(interpolated, "{{GovernmentEmblem}}", emblemBase64);
            interpolated = interpolated.Replace("{{GovernmentEmblem}}", emblemTag, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{FamilyCardNumber}}", familyCardNumber, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{HeadOfHouseholdName}}", headOfHouseholdName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{HeadOfHouseholdNic}}", headOfHouseholdNic, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{HouseNumber}}", houseNumber ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{Address}}", address, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{GnDivisionId}}", divisionId, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{GnDivisionName}}", gnDivisionName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{DsDivisionName}}", dsDivisionName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{DistrictName}}", districtName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{RegisteredDate}}", registeredDate, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{FamilyCardStatus}}", familyCardStatus, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{IssuedDate}}", editable.IssuedDate ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{GnOfficerName}}", gnOfficerName, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{FamilyMembersRows}}", membersHtmlBuilder.ToString(), StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{ActiveLivingMemberCount}}", members.Count.ToString(), StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{TotalMonthlyIncome}}", $"{totalIncome:N2}", StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{QrCode}}", qrBase64Tag, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{OfficerSignature}}", sigTag, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("{{RubberStamp}}", stampTag, StringComparison.OrdinalIgnoreCase);
            interpolated = interpolated.Replace("&quot;", "\"");

            return RenderHtmlToPdf(interpolated);
        }

        /// <summary>
        /// Renders a complete HTML document (including inline styles) to a PDF byte array
        /// using PuppeteerSharp (headless Chromium). The HTML from Certificate_Templates
        /// drives the full layout — no hardcoded drawing code.
        /// </summary>
        private byte[] RenderHtmlToPdf(string bodyHtml)
        {
            // Wrap bare template content in a full HTML document with A4 print styles
            string fullHtml = "<!DOCTYPE html>" + Environment.NewLine +
                "<html lang=\"en\">" + Environment.NewLine +
                "<head>" + Environment.NewLine +
                "<meta charset=\"UTF-8\" />" + Environment.NewLine +
                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />" + Environment.NewLine +
                "<style>" + Environment.NewLine +
                "  * { box-sizing: border-box; margin: 0; padding: 0; }" + Environment.NewLine +
                "  html, body { width: 100%; background: #fff; }" + Environment.NewLine +
                "  @media print { @page { size: A4; margin: 0; } body { margin: 0; } }" + Environment.NewLine +
                "</style></head><body>" + Environment.NewLine +
                bodyHtml + Environment.NewLine +
                "</body></html>";

            // Use PuppeteerSharp to render HTML to PDF with headless Chromium
            return Task.Run(async () =>
            {
                var launchOptions = new PuppeteerSharp.LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
                };

                // Find local Chrome or Edge on Windows to bypass network download failure
                string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
                string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

                if (System.IO.File.Exists(chromePath))
                {
                    launchOptions.ExecutablePath = chromePath;
                }
                else if (System.IO.File.Exists(edgePath))
                {
                    launchOptions.ExecutablePath = edgePath;
                }

                await using var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(launchOptions);

                await using var page = await browser.NewPageAsync();
                await page.SetContentAsync(fullHtml);
                await page.EvaluateExpressionHandleAsync("document.fonts.ready");



                using var pdfStream = await page.PdfStreamAsync(new PuppeteerSharp.PdfOptions
                {
                    Format = PuppeteerSharp.Media.PaperFormat.A4,
                    PrintBackground = true,
                    MarginOptions = new PuppeteerSharp.Media.MarginOptions
                    {
                        Top = "0mm",
                        Bottom = "0mm",
                        Left = "0mm",
                        Right = "0mm"
                    }
                });

                using var ms = new MemoryStream();
                await pdfStream.CopyToAsync(ms);
                return ms.ToArray();
            }).GetAwaiter().GetResult();
        }


        private static string MapStatusToTitle(string status)
        {
            return status.ToUpper() switch
            {
                "SUBMITTED" or "PENDING" => "Submitted",
                "UNDER_REVIEW" or "IN PROGRESS" or "IN_PROGRESS" => "Under Review",
                "VERIFIED" => "Verified",
                "ACCEPTED" or "APPROVED" => "Approved",
                "REJECTED" => "Rejected",
                "REVOKED" => "Revoked",
                "CHANGES_REQUESTED" => "Changes Requested",
                "RESUBMITTED" => "Resubmitted",
                _ => status
            };
        }

        private static List<string> SafeGetJsonArray(string? json, string propertyName)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(json)) return list;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in prop.EnumerateArray())
                        {
                            var val = item.GetString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                list.Add(val);
                            }
                        }
                    }
                    else if (prop.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var val = prop.GetString();
                        if (!string.IsNullOrEmpty(val))
                        {
                            list.AddRange(val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        }
                    }
                }
            }
            catch {}
            return list;
        }

        private static string SafeGetJsonProperty(string? json, string propertyName)
        {
            if (string.IsNullOrEmpty(json)) return string.Empty;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(propertyName, out var prop))
                {
                    return prop.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => prop.GetString() ?? string.Empty,
                        System.Text.Json.JsonValueKind.Number => prop.GetRawText(),
                        _ => prop.ToString()
                    };
                }
            }
            catch {}
            return string.Empty;
        }
        [HttpPut("remark/{id}")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> EditLatestRemark(int id, [FromBody] EditRemarkDto dto)

        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var history = await _context.RequestStatusHistories
                .Include(h => h.Request)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (history == null) return NotFound(new { message = "Remark not found." });

            if (history.Request.AssignedGnOfficerNic != gnNic)
            {
                return Forbid();
            }

            var statusUpper = history.Request.CurrentStatus?.ToUpper();
            if (statusUpper == "ACCEPTED" || statusUpper == "APPROVED" || statusUpper == "REJECTED")
            {
                return BadRequest(new { message = "Remarks cannot be edited for requests that are Accepted or Rejected." });
            }

            var latestRemarkHistory = await _context.RequestStatusHistories
                .Where(h => h.RequestId == history.RequestId && !string.IsNullOrEmpty(h.Remarks))
                .OrderByDescending(h => h.ChangedDate)
                .FirstOrDefaultAsync();

            if (latestRemarkHistory == null || latestRemarkHistory.Id != id)
            {
                return BadRequest(new { message = "Only the most recent remark can be edited." });
            }

            if (string.IsNullOrWhiteSpace(dto.Remarks))
            {
                return BadRequest(new { message = "Remark text cannot be empty." });
            }

            history.Remarks = dto.Remarks;
            _context.RequestStatusHistories.Update(history);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Remark updated successfully." });
        }
    }

    public class EditRemarkDto
    {
        public string Remarks { get; set; }
    }

    public class ReleaseRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class UpdateStatusRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class CertificateEditorDataResponseDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Language { get; set; } = "English";
        public string RequestNumber { get; set; } = string.Empty;
        public string CitizenName { get; set; } = string.Empty;
        public string CitizenNic { get; set; } = string.Empty;
        public string ApplicantCategory { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TemplateContent { get; set; } = string.Empty;
        public string? RubberStampUrl { get; set; }
        public string? OfficerSignatureUrl { get; set; }
        public string GnDivisionName { get; set; } = string.Empty;
        public string GnOfficerName { get; set; } = string.Empty;
        public string GnDivisionId { get; set; } = string.Empty;
        public string DsDivisionName { get; set; } = string.Empty;
        public string DistrictName { get; set; } = string.Empty;
        public CertificateEditableValuesDto EditableValues { get; set; } = new();

        // Additional properties for Family Card Category ID 1
        public int CategoryId { get; set; }
        public string? FamilyCardNumber { get; set; }
        public string? HeadOfHouseholdName { get; set; }
        public string? HeadOfHouseholdNic { get; set; }
        public string? HouseNumber { get; set; }
        public string? Address { get; set; }
        public string? RegisteredDate { get; set; }
        public string? FamilyCardStatus { get; set; }
        public string? FamilyMembersRows { get; set; }
        public string? ActiveLivingMemberCount { get; set; }
        public string? TotalMonthlyIncome { get; set; }
    }

    public class CertificateEditableValuesDto
    {
        public string CitizenFullName { get; set; } = string.Empty;
        public string CitizenNic { get; set; } = string.Empty;
        public string ResidentialAddress { get; set; } = string.Empty;
        public string RequiredFor { get; set; } = string.Empty;
        public string IssuedDate { get; set; } = string.Empty;
        public string CertificateName { get; set; } = string.Empty;


        public string ResidenceFromDate { get; set; } = string.Empty;
        public string ResidencePeriod { get; set; } = string.Empty;
        public string TemporaryAddress { get; set; } = string.Empty;
        public string TemporaryFromDate { get; set; } = string.Empty;
        public string TemporaryToDate { get; set; } = string.Empty;
        public string TemporaryResidenceReason { get; set; } = string.Empty;
        public string DeceasedFullName { get; set; } = string.Empty;
        public string DeceasedNic { get; set; } = string.Empty;
        public string DeceasedAddress { get; set; } = string.Empty;
        public string DateOfDeath { get; set; } = string.Empty;
        public string PlaceOfDeath { get; set; } = string.Empty;
        public string ReasonOfDeath { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
        public string MonthlyIncome { get; set; } = string.Empty;
        public string AnnualIncome { get; set; } = string.Empty;
        public string CertificateBody { get; set; } = string.Empty;
    }

    public class GenerateCertificateRequestDto
    {
        public CertificateEditableValuesDto EditableValues { get; set; } = new();
        public string? Remarks { get; set; }
    }

    public class StartReviewRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class FinalizeRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class FamilyMemberRowDto
    {
        public string Nic { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Dob { get; set; } = string.Empty;
        public string? Occupation { get; set; }
        public decimal MonthlyIncome { get; set; }
    }
}

