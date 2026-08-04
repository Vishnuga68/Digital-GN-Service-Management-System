using EGN_Service_Management.Database;
using EGN_Service_Management.DTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Services
{
    public class CitizenRequestService : ICitizenRequestService
    {
        private readonly EgnDbContext _dbContext;
        private readonly IWebHostEnvironment _env;

        public CitizenRequestService(EgnDbContext dbContext, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _env = env;
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

        public async Task<string> SubmitRequestAsync(CitizenRequestSubmissionDTO requestDto, string currentUserId)
        {
            var requestIdStr = $"REQ-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Fetch category details to check if it's Document Verification
                var category = await _dbContext.ServiceCategories
                    .Include(c => c.Service)
                    .FirstOrDefaultAsync(c => c.CategoryId == requestDto.CategoryId);
                
                bool isDocVerification = category != null && 
                    (category.CategoryId == 43 || 
                     category.CategoryName.Contains("Document Verification", StringComparison.OrdinalIgnoreCase) ||
                     (category.Service != null && category.Service.ServiceName.Contains("Document Verification", StringComparison.OrdinalIgnoreCase)));

                var gnDivisionId = requestDto.GnDivisionId;
                if (requestDto.CategoryId == 5 || requestDto.CategoryId == 42)
                {
                    var submitterCitizen = await _dbContext.CitizenMasters
                        .FirstOrDefaultAsync(c => c.Nic == (currentUserId ?? requestDto.CitizenNic));
                    if (submitterCitizen != null && !string.IsNullOrEmpty(submitterCitizen.GnDivisionId))
                    {
                        gnDivisionId = submitterCitizen.GnDivisionId;
                        requestDto.GnDivisionId = gnDivisionId;
                    }
                }
                if (requestDto.CategoryId == 2)
                {
                    var submitterCitizen = await _dbContext.CitizenMasters
                        .FirstOrDefaultAsync(c => c.Nic == currentUserId);
                    if (submitterCitizen == null)
                    {
                        throw new ArgumentException("Submitter citizen record not found.");
                    }
                    if (string.IsNullOrEmpty(submitterCitizen.FamilyCardNumber))
                    {
                        throw new ArgumentException("Submitter does not belong to a family card.");
                    }
                    var submitterFamilyCard = await _dbContext.FamilyCards
                        .FirstOrDefaultAsync(fc => fc.FamilyCardNumber == submitterCitizen.FamilyCardNumber && fc.Status == "Active");
                    if (submitterFamilyCard == null)
                    {
                        throw new ArgumentException("Submitter's active family card is not found or inactive.");
                    }
                    gnDivisionId = submitterFamilyCard.GnDivisionId;
                    requestDto.CitizenNic = currentUserId;
                    requestDto.GnDivisionId = gnDivisionId;
                }

                if (requestDto.CategoryId == 7)
                {
                    var citizen = await _dbContext.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == requestDto.CitizenNic);
                    if (citizen == null)
                    {
                        throw new ArgumentException("Citizen record not found.");
                    }

                    var today = DateOnly.FromDateTime(GetSriLankanTime());
                    var birthDate = citizen.Dob;
                    var age = today.Year - birthDate.Year;

                    if (age < 18)
                    {
                        throw new ArgumentException("Citizen must be at least 18 years old in the registration year to register as a voter.");
                    }

                    if (string.IsNullOrEmpty(citizen.LifeStatus) || !string.Equals(citizen.LifeStatus, "Alive", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("Citizen is not alive.");
                    }

                    var isAlreadyRegistered = await _dbContext.VoterRegistries.AnyAsync(vr => vr.CitizenNic == requestDto.CitizenNic);
                    if (isAlreadyRegistered)
                    {
                        throw new ArgumentException("Citizen is already registered in the voter registry.");
                    }

                    var hasPendingRequest = await _dbContext.UnifiedRequests.AnyAsync(r =>
                        r.CitizenNic == requestDto.CitizenNic &&
                        r.CategoryId == 7 &&
                        r.CurrentStatus != "Accepted" &&
                        r.CurrentStatus != "Approved" &&
                        r.CurrentStatus != "Rejected");
                    if (hasPendingRequest)
                    {
                        throw new ArgumentException("A pending voter registration request already exists for this citizen.");
                    }
                }

                // Get GN Officer NIC for the division
                var gnOfficerNic = await _dbContext.GnOfficers
                    .Where(g => g.GnDivisionId == gnDivisionId)
                    .Select(g => g.Nic)
                    .FirstOrDefaultAsync();

                // 2. Validate and Process Main Document if Document Verification
                string? originalFileHash = null;
                UnifiedRequestAttachment? mainAttachment = null;
                var initialStatus = "Submitted";

                if (isDocVerification)
                {
                    if (requestDto.MainDocument == null || requestDto.MainDocument.Length == 0)
                    {
                        throw new ArgumentException("Exactly one main document is required for Document Verification.");
                    }

                    var file = requestDto.MainDocument;
                    
                    // Validate File Size (Max 5MB)
                    if (file.Length > 5 * 1024 * 1024)
                    {
                        throw new ArgumentException("Maximum file size allowed is 5MB.");
                    }

                    // Validate File Extension
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        throw new ArgumentException("Only PDF, JPG, JPEG, and PNG files are allowed.");
                    }

                    // Validate MIME Type
                    var allowedMimeTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
                    if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                    {
                        throw new ArgumentException("Invalid file type.");
                    }

                    // Validate Binary File Signature
                    if (!ValidateFileSignature(file))
                    {
                        throw new ArgumentException("File content signature verification failed. The file is corrupt or invalid.");
                    }

                    // Calculate SHA-256 Hash
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            var hashBytes = sha256.ComputeHash(stream);
                            originalFileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }

                // 3. Create UnifiedRequest
                var unifiedRequest = new UnifiedRequest
                {
                    CitizenNic = requestDto.CitizenNic,
                    ServiceId = requestDto.ServiceId,
                    CategoryId = requestDto.CategoryId,
                    CurrentStatus = initialStatus,
                    SubmittedDate = GetSriLankanTime(),
                    LastUpdated = GetSriLankanTime(),
                    AssignedGnOfficerNic = gnOfficerNic,
                    GnDivision = requestDto.GnDivisionId,
                    RequestNumber = requestIdStr
                };
                await _dbContext.UnifiedRequests.AddAsync(unifiedRequest);
                await _dbContext.SaveChangesAsync(); // To generate UnifiedRequest.RequestId

                // 4. Create RequestData
                if (!string.IsNullOrEmpty(requestDto.JsonData))
                {
                    var requestData = new RequestData
                    {
                        RequestId = unifiedRequest.RequestId,
                        JsonData = requestDto.JsonData,
                        CreatedDate = GetSriLankanTime()
                    };
                    await _dbContext.RequestDataEntries.AddAsync(requestData);
                }

                // 5. Create RequestStatusHistory
                var statusHistory = new RequestStatusHistory
                {
                    RequestId = unifiedRequest.RequestId,
                    Status = initialStatus,
                    Remarks = null,
                    ChangedBy = currentUserId ?? requestDto.CitizenNic,
                    ChangedDate = GetSriLankanTime()
                };
                await _dbContext.RequestStatusHistories.AddAsync(statusHistory);

                // 6. Process Attachments
                var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // If Document Verification, process main document
                if (isDocVerification && requestDto.MainDocument != null)
                {
                    var file = requestDto.MainDocument;
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var secureFileName = $"DV_{requestDto.CitizenNic}_{DateTime.UtcNow.Ticks}{extension}";
                    var filePath = Path.Combine(uploadFolder, secureFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var fileUrl = $"/uploads/requests/{secureFileName}";
                    mainAttachment = new UnifiedRequestAttachment
                    {
                        RequestId = unifiedRequest.RequestId,
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileType = file.ContentType,
                        UploadedAt = GetSriLankanTime(),
                        DocumentRole = "MAIN_DOCUMENT",
                        DisplayName = "Document to be verified",
                        IsPrimary = true,
                        UploadedBy = currentUserId ?? requestDto.CitizenNic,
                        FileSize = file.Length,
                        MimeType = file.ContentType,
                        IsActive = true
                    };
                    await _dbContext.UnifiedRequestAttachments.AddAsync(mainAttachment);
                    await _dbContext.SaveChangesAsync(); // Generate AttachmentId for main document
                }

                // Process Supporting Attachments
                if (requestDto.Attachments != null && requestDto.Attachments.Count > 0)
                {
                    foreach (var file in requestDto.Attachments)
                    {
                        if (file.Length > 0)
                        {
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            var filePath = Path.Combine(uploadFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var fileUrl = $"/uploads/requests/{uniqueFileName}";

                            var attachment = new UnifiedRequestAttachment
                            {
                                RequestId = unifiedRequest.RequestId,
                                FileName = file.FileName,
                                FileUrl = fileUrl,
                                FileType = file.ContentType,
                                UploadedAt = GetSriLankanTime(),
                                DocumentRole = "SUPPORTING_DOCUMENT",
                                DisplayName = file.FileName,
                                IsPrimary = false,
                                UploadedBy = currentUserId ?? requestDto.CitizenNic,
                                FileSize = file.Length,
                                MimeType = file.ContentType,
                                IsActive = true
                            };
                            await _dbContext.UnifiedRequestAttachments.AddAsync(attachment);
                        }
                    }
                }

                // 7. Create Document Verification Record if Document Verification
                if (isDocVerification && mainAttachment != null)
                {
                    var docVerification = new DocumentVerification
                    {
                        RequestId = unifiedRequest.RequestId,
                        MainAttachmentId = mainAttachment.AttachmentId,
                        AssignedGNOfficerNIC = gnOfficerNic,
                        VerificationStatus = "PENDING",
                        OriginalFileHash = originalFileHash,
                        CreatedDate = GetSriLankanTime()
                    };
                    await _dbContext.DocumentVerifications.AddAsync(docVerification);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return unifiedRequest.RequestNumber;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task HandleCitizenGnDivisionChangeAsync(string citizenNic, string oldGnDivisionId, string newGnDivisionId, string oldFamilyCardNumber, string newGnOfficerNic, string triggeredBy, int? excludeRequestId = null)
        {
            if (!string.IsNullOrEmpty(oldFamilyCardNumber))
            {
                var remainingAliveMembers = await _dbContext.CitizenMasters
                    .CountAsync(c => c.FamilyCardNumber == oldFamilyCardNumber && c.LifeStatus == "Alive" && c.Nic != citizenNic);
                
                if (remainingAliveMembers == 0)
                {
                    var oldFamilyCard = await _dbContext.FamilyCards.FirstOrDefaultAsync(fc => fc.FamilyCardNumber == oldFamilyCardNumber);
                    if (oldFamilyCard != null)
                    {
                        oldFamilyCard.Status = "Inactive";
                        _dbContext.FamilyCards.Update(oldFamilyCard);
                    }
                }
            }

            if (oldGnDivisionId == newGnDivisionId)
            {
                // No division change occurred, open requests stay with the same GN division
                // Return from the GN reassignment part of the reusable method after the family-card check is completed.
                await _dbContext.SaveChangesAsync();
                return;
            }

            var openRequests = await _dbContext.UnifiedRequests
                .Where(r => r.CitizenNic == citizenNic &&
                            (r.CurrentStatus == "Submitted" || r.CurrentStatus == "SUBMITTED" ||
                             r.CurrentStatus == "Pending" || r.CurrentStatus == "PENDING" ||
                             r.CurrentStatus == "Under Review" || r.CurrentStatus == "UNDER_REVIEW" ||
                             r.CurrentStatus == "Verified" || r.CurrentStatus == "VERIFIED") &&
                             r.GnDivision != newGnDivisionId &&
                             (!excludeRequestId.HasValue || r.RequestId != excludeRequestId.Value))
                .ToListAsync();

            foreach (var req in openRequests)
            {
                req.GnDivision = newGnDivisionId;
                req.AssignedGnOfficerNic = newGnOfficerNic;
                req.LastUpdated = GetSriLankanTime();

                var statusRemark = $"Request reassigned from GN division {oldGnDivisionId ?? "Unknown"} to {newGnDivisionId} due to citizen GN division transfer.";

                var verification = await _dbContext.DocumentVerifications
                    .FirstOrDefaultAsync(v => v.RequestId == req.RequestId);

                if (verification != null)
                {
                    if ((req.CurrentStatus == "Verified" || req.CurrentStatus == "VERIFIED") && verification.VerificationStatus == "GENERATED")
                    {
                        if (verification.FinalVerifiedAttachmentId.HasValue)
                        {
                            var generatedAtt = await _dbContext.UnifiedRequestAttachments
                                .FirstOrDefaultAsync(a => a.AttachmentId == verification.FinalVerifiedAttachmentId.Value);

                            if (generatedAtt != null)
                            {
                                generatedAtt.DocumentRole = "SUPERSEDED_CERTIFICATE";
                                generatedAtt.IsActive = false;
                                _dbContext.UnifiedRequestAttachments.Update(generatedAtt);
                            }
                        }

                        verification.AssignedGNOfficerNIC = newGnOfficerNic;
                        verification.VerificationStatus = "UNDER_REVIEW";
                        verification.VerificationRemarks = "Previously generated document invalidated due to GN division reassignment. Re-verification and regeneration are required by the newly assigned GN Officer.";
                        verification.VerifiedDate = null;
                        verification.FinalVerifiedAttachmentId = null;
                        verification.VerificationTokenHash = null;
                        verification.VerificationUrl = null;
                        verification.FinalFileHash = null;
                        verification.UpdatedDate = GetSriLankanTime();
                        _dbContext.DocumentVerifications.Update(verification);

                        req.CurrentStatus = "Under Review";
                        req.ApprovedDate = null;
                        req.FinalDocumentUrl = null;

                        statusRemark = $"Request reassigned from GN division {oldGnDivisionId ?? "Unknown"} to {newGnDivisionId}. Previously generated document was not published and has been invalidated. Re-verification and regeneration are required by the newly assigned GN Officer.";
                    }
                    else
                    {
                        verification.AssignedGNOfficerNIC = newGnOfficerNic;
                        verification.UpdatedDate = GetSriLankanTime();
                        _dbContext.DocumentVerifications.Update(verification);
                    }
                }

                _dbContext.UnifiedRequests.Update(req);

                var statusHistory = new RequestStatusHistory
                {
                    RequestId = req.RequestId,
                    Status = req.CurrentStatus,
                    Remarks = statusRemark,
                    ChangedBy = triggeredBy,
                    ChangedDate = GetSriLankanTime()
                };
                await _dbContext.RequestStatusHistories.AddAsync(statusHistory);
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}
