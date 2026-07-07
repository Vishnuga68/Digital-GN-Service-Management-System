using EGN_Service_Management.DTO;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EGN_Service_Management.Database;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitizenRequestController : ControllerBase
    {
        private readonly ICitizenRequestService _citizenRequestService;
        private readonly EgnDbContext _context;

        public CitizenRequestController(ICitizenRequestService citizenRequestService, EgnDbContext context)
        {
            _citizenRequestService = citizenRequestService;
            _context = context;
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
                    submittedDate = r.SubmittedDate.ToString("yyyy-MM-dd")
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
                    submittedDate = r.SubmittedDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("details/{requestNumber}")]
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
                    .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber);

                if (request == null)
                {
                    return NotFound(new { message = "Request not found" });
                }

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

                // Determine current step
                int currentStep = 2; // Default to In Progress
                if (request.CurrentStatus == "Pending") currentStep = 1;
                if (request.CurrentStatus == "Approved" || request.CurrentStatus == "Rejected" || request.CurrentStatus == "Accepted") currentStep = 4;
                else if (request.CurrentStatus == "Verified") currentStep = 3;

                var timeline = new System.Collections.Generic.List<EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO>
                {
                    new EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO { Step = 1, Title = "Submitted", Timestamp = request.SubmittedDate, Completed = true, Officer = "" },
                    new EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO { Step = 2, Title = "Under Review", Timestamp = currentStep >= 2 ? request.LastUpdated : null, Completed = currentStep >= 2, Officer = request.AssignedOfficer?.FullName ?? "" },
                    new EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO { Step = 3, Title = "Verified", Timestamp = currentStep >= 3 ? request.LastUpdated : null, Completed = currentStep >= 3, Officer = request.AssignedOfficer?.FullName ?? "" },
                    new EGN_Service_Management.DTO.RequestDTO.TimelineStepDTO { 
                        Step = 4, 
                        Title = request.CurrentStatus == "Rejected" ? "Rejected" : (request.CurrentStatus == "Approved" || request.CurrentStatus == "Accepted" ? "Approved" : "Final Decision"), 
                        Timestamp = currentStep == 4 ? request.LastUpdated : null, 
                        Completed = currentStep == 4, 
                        Officer = request.AssignedOfficer?.FullName ?? ""
                    }
                };

                var response = new EGN_Service_Management.DTO.RequestDTO.RequestDetailsResponseDTO
                {
                    RequestId = request.RequestNumber,
                    ServiceType = request.Service?.ServiceName ?? "Unknown",
                    Category = request.Category?.CategoryName ?? "Unknown",
                    SubmittedDate = request.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    LastUpdated = request.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ss"),
                    CurrentStatus = request.CurrentStatus ?? "Unknown",
                    ApprovedDate = request.ApprovedDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    RejectedDate = request.RejectedDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Applicant = request.Citizen != null ? new EGN_Service_Management.DTO.RequestDTO.ApplicantDTO
                    {
                        FullName = request.Citizen.FullName ?? "Unknown",
                        Nic = request.CitizenNic ?? "Unknown",
                        Address = request.Citizen.FamilyCard?.Address ?? "Unknown Address"
                    } : new EGN_Service_Management.DTO.RequestDTO.ApplicantDTO { FullName = "", Nic = "", Address = "" },
                    Attachments = request.Attachments?.Select(a => new EGN_Service_Management.DTO.RequestDTO.AttachmentDTO
                    {
                        FileName = a.FileName,
                        FileUrl = a.FileUrl,
                        FileType = a.FileType,
                        UploadedDate = a.UploadedAt,
                        FileSize = "Unknown" // We don't store file size in DB right now
                    }).ToList() ?? new System.Collections.Generic.List<EGN_Service_Management.DTO.RequestDTO.AttachmentDTO>(),
                    DynamicFields = dynamicFields,
                    Remarks = new System.Collections.Generic.List<EGN_Service_Management.DTO.RequestDTO.RemarkDTO>(),
                    Timeline = timeline
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace, innerException = ex.InnerException?.Message });
            }
        }
    }
}
