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

        public async Task<string> SubmitRequestAsync(CitizenRequestSubmissionDTO requestDto, string currentUserId)
        {
            var requestIdStr = $"REQ-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Get GN Officer NIC for the division
                var gnOfficerNic = await _dbContext.GnOfficers
                    .Where(g => g.GnDivisionId == requestDto.GnDivisionId)
                    .Select(g => g.Nic)
                    .FirstOrDefaultAsync();

                // 2. Create UnifiedRequest
                var unifiedRequest = new UnifiedRequest
                {
                    CitizenNic = requestDto.CitizenNic,
                    ServiceId = requestDto.ServiceId,
                    CategoryId = requestDto.CategoryId,
                    CurrentStatus = "Pending",
                    SubmittedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    AssignedGnOfficerNic = gnOfficerNic,
                    GnDivision = requestDto.GnDivisionId,
                    RequestNumber = requestIdStr
                };
                await _dbContext.UnifiedRequests.AddAsync(unifiedRequest);
                await _dbContext.SaveChangesAsync(); // To generate UnifiedRequest.RequestId

                // 3. Create RequestData
                if (!string.IsNullOrEmpty(requestDto.JsonData))
                {
                    var requestData = new RequestData
                    {
                        RequestId = unifiedRequest.RequestId,
                        JsonData = requestDto.JsonData,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _dbContext.RequestDataEntries.AddAsync(requestData);
                }

                // 4. Create RequestStatusHistory
                var statusHistory = new RequestStatusHistory
                {
                    RequestId = unifiedRequest.RequestId,
                    Status = "Pending",
                    Remarks = "Request submitted by citizen",
                    ChangedBy = currentUserId ?? requestDto.CitizenNic,
                    ChangedDate = DateTime.UtcNow
                };
                await _dbContext.RequestStatusHistories.AddAsync(statusHistory);

                // 5. Process Attachments
                if (requestDto.Attachments != null && requestDto.Attachments.Count > 0)
                {
                    var uploadFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "requests");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

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
                                UploadedAt = DateTime.UtcNow
                            };
                            await _dbContext.UnifiedRequestAttachments.AddAsync(attachment);
                        }
                    }
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
    }
}
