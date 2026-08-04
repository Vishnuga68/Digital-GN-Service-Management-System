using EGN_Service_Management.Database;
using EGN_Service_Management.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly EgnDbContext _context;

        public DashboardController(EgnDbContext context)
        {
            _context = context;
        }

        [HttpGet("citizen/{nic}")]
        [Authorize(Roles = "Citizen,DSOfficer,GNOfficer")]
        public async Task<IActionResult> GetCitizenDashboard(string nic)
        {
            try
            {
                var dbConnection = _context.Database.GetDbConnection();
                var wasOpen = dbConnection.State == ConnectionState.Open;
                if (!wasOpen) await _context.Database.OpenConnectionAsync();

                try
                {
                    // 1. Fetch profile and division hierarchy info via JOINs
                    var profile = new Dictionary<string, object>();
                    using (var command = dbConnection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                cm.[Nic], cm.[FullName], cm.[Dob], cm.[Gender], cm.[MaritalStatus], 
                                cm.[Religion], cm.[Occupation], CASE WHEN cm.[Nic] = fc.[HeadOfHouseholdNic] THEN 'Head' ELSE 'Member' END AS RelationshipToHead, cm.[LifeStatus],
                                cm.[PastFamilyCardNumber], fc.[FamilyCardNumber], fc.[Address], fc.[Status] AS FamilyCardStatus,
                                gd.[Id] AS GnDivisionId, gd.[Name] AS GnDivisionName,
                                dd.[Id] AS DsDivisionId, dd.[Name] AS DsDivisionName,
                                a.[Email], a.[Phone], a.[ProfileImage] AS [Profile],
                                gno.[FullName] AS GnOfficerName
                            FROM [Citizen_Master] cm
                            LEFT JOIN [accounts] a ON cm.[Nic] = a.[Nic]
                            LEFT JOIN [family_cards] fc ON cm.[FamilyCardNumber] = fc.[FamilyCardNumber]
                            LEFT JOIN [gn_divisions] gd ON COALESCE(fc.[GnDivisionId], cm.[GnDivisionId]) = gd.[Id]
                            LEFT JOIN [ds_divisions] dd ON gd.[DsDivisionId] = dd.[Id]
                            LEFT JOIN [gn_officers] gno ON gd.[Id] = gno.[GnDivisionId]
                            WHERE cm.[Nic] = @Nic";

                        var param = command.CreateParameter();
                        param.ParameterName = "@Nic";
                        param.Value = nic;
                        command.Parameters.Add(param);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    profile[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                            }
                        }
                    }

                    if (profile.Count == 0)
                    {
                        return NotFound(ApiResponse<object>.ErrorResponse("Citizen profile not found in master records."));
                    }

                    // 2. Fetch service requests statistics
                    var stats = new Dictionary<string, object>
                    {
                        { "TotalRequests", 0 },
                        { "PendingRequests", 0 },
                        { "InProgressRequests", 0 },
                        { "ApprovedRequests", 0 },
                        { "RejectedRequests", 0 },
                        { "NewMessages", 0 },
                        { "UpcomingMeetings", 0 },
                        { "WeeklyActivity", new int[7] },
                        { "WeeklyActivityLabels", new string[7] },
                        { "RecentActivities", new List<object>() }
                    };

                    var requestsList = await _context.UnifiedRequests
                        .Where(r => r.CitizenNic == nic)
                        .ToListAsync();

                    int totalRequests = requestsList.Count;

                    // Exact grouping logic matching the Request page cards/filter
                    int pendingRequests = requestsList.Count(r => 
                        new[] { "pending", "submitted" }
                        .Contains(r.CurrentStatus.ToLower().Trim()));

                    int inProgressRequests = requestsList.Count(r => 
                        new[] { "under review", "under_review", "verified", "in progress", "changes requested", "resubmitted", "forwardedtodestinationgn", "returned" }
                        .Contains(r.CurrentStatus.ToLower().Trim()));

                    int approvedRequests = requestsList.Count(r => 
                        new[] { "approved", "accepted" }
                        .Contains(r.CurrentStatus.ToLower().Trim()));

                    int rejectedRequests = requestsList.Count(r => 
                        new[] { "rejected", "revoked" }
                        .Contains(r.CurrentStatus.ToLower().Trim()));

                    stats["TotalRequests"] = totalRequests;
                    stats["PendingRequests"] = pendingRequests;
                    stats["InProgressRequests"] = inProgressRequests;
                    stats["ApprovedRequests"] = approvedRequests;
                    stats["RejectedRequests"] = rejectedRequests;

                    // Unread Messages Count
                    int newMessages = await _context.NewChatMessages
                        .CountAsync(m => m.Conversation.CitizenNic == nic && m.SenderNic != nic && !m.IsRead && !m.IsDeleted);
                    stats["NewMessages"] = newMessages;

                    // Upcoming Meetings Count
                    TimeZoneInfo slZone;
                    try {
                        slZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
                    } catch {
                        slZone = TimeZoneInfo.CreateCustomTimeZone("SLST", TimeSpan.FromHours(5.5), "Sri Lanka Standard Time", "Sri Lanka Standard Time");
                    }
                    var slNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, slZone);
                    var slTodayDateOnly = DateOnly.FromDateTime(slNow);

                    int upcomingMeetings = await _context.Appointments
                        .CountAsync(a => a.CitizenNic == nic && a.AppointmentDate >= slTodayDateOnly && a.Status == "BOOKED");
                    stats["UpcomingMeetings"] = upcomingMeetings;

                    // Weekly Activity Breakdown (for the last seven days)
                    var slToday = slNow.Date;
                    var startDate = slToday.AddDays(-6);
                    var weeklyActivity = new int[7];
                    var weeklyActivityLabels = new string[7];

                    // Fetch timestamps for the last 7 days in bulk (only 6 queries instead of 42)
                    var reqDates = await _context.UnifiedRequests
                        .Where(r => r.CitizenNic == nic && r.SubmittedDate >= startDate)
                        .Select(r => r.SubmittedDate)
                        .ToListAsync();

                    var histDates = await _context.RequestStatusHistories
                        .Where(h => h.Request.CitizenNic == nic && h.ChangedDate >= startDate)
                        .Select(h => h.ChangedDate)
                        .ToListAsync();

                    var appDates = await _context.Appointments
                        .Where(a => a.CitizenNic == nic && a.CreatedAt >= startDate)
                        .Select(a => a.CreatedAt)
                        .ToListAsync();

                    var chatDates = await _context.NewChatMessages
                        .Where(m => m.SenderNic == nic && m.SentDate >= startDate)
                        .Select(m => m.SentDate)
                        .ToListAsync();

                    var docVerifDates = await _context.DocumentVerifications
                        .Where(dv => dv.Request.CitizenNic == nic && dv.CreatedDate >= startDate)
                        .Select(dv => dv.CreatedDate)
                        .ToListAsync();

                    var vaultDocDates = await _context.DocumentVaultDocuments
                        .Where(vd => vd.NIC == nic && vd.UploadedAt >= startDate)
                        .Select(vd => vd.UploadedAt)
                        .ToListAsync();

                    for (int i = 0; i < 7; i++)
                    {
                        var targetDate = slToday.AddDays(-6 + i);
                        weeklyActivityLabels[i] = targetDate.ToString("ddd"); // e.g. "Mon", "Tue"
                        
                        int reqCount = reqDates.Count(d => d.Date == targetDate);
                        int histCount = histDates.Count(d => d.Date == targetDate);
                        int appCount = appDates.Count(d => d.Date == targetDate);
                        int chatCount = chatDates.Count(d => d.Date == targetDate);
                        int docVerifCount = docVerifDates.Count(d => d.Date == targetDate);
                        int vaultDocCount = vaultDocDates.Count(d => d.Date == targetDate);

                        weeklyActivity[i] = reqCount + histCount + appCount + chatCount + docVerifCount + vaultDocCount;
                    }
                    stats["WeeklyActivity"] = weeklyActivity;
                    stats["WeeklyActivityLabels"] = weeklyActivityLabels;

                    // Recent Activities (merging latest 10 records from 6 tables)
                    var reqs = await _context.UnifiedRequests
                        .Include(r => r.Category)
                        .Where(r => r.CitizenNic == nic)
                        .OrderByDescending(r => r.SubmittedDate)
                        .Take(10)
                        .Select(r => new {
                            Date = r.SubmittedDate,
                            Action = "Submitted Request",
                            Status = r.CurrentStatus,
                            Detail = (r.Category != null ? r.Category.CategoryName : "Service Request") + " (" + r.RequestNumber + ")"
                        })
                        .ToListAsync();

                    var statusHistories = await _context.RequestStatusHistories
                        .Include(h => h.Request)
                        .Where(h => h.Request.CitizenNic == nic)
                        .OrderByDescending(h => h.ChangedDate)
                        .Take(10)
                        .Select(h => new {
                            Date = h.ChangedDate,
                            Action = "Request Status Changed",
                            Status = h.Status,
                            Detail = "Request " + h.Request.RequestNumber + " status changed to " + h.Status + (string.IsNullOrEmpty(h.Remarks) ? "" : ". Remarks: " + h.Remarks)
                        })
                        .ToListAsync();

                    var appointments = await _context.Appointments
                        .Where(a => a.CitizenNic == nic)
                        .OrderByDescending(a => a.CreatedAt)
                        .Take(10)
                        .Select(a => new {
                            Date = a.CreatedAt,
                            Action = "Booked Appointment",
                            Status = a.Status,
                            Detail = a.ServiceType + " meeting scheduled for " + a.AppointmentDate.ToString("yyyy-MM-dd")
                        })
                        .ToListAsync();

                    var messagesList = await _context.NewChatMessages
                        .Include(m => m.Conversation)
                        .Where(m => m.Conversation.CitizenNic == nic)
                        .OrderByDescending(m => m.SentDate)
                        .Take(10)
                        .Select(m => new {
                            Date = m.SentDate,
                            Action = "Chat Message",
                            Status = "Info",
                            Detail = (m.SenderNic == nic ? "You" : "GN Officer") + ": " + (m.MessageText ?? "Sent an attachment")
                        })
                        .ToListAsync();

                    var verifications = await _context.DocumentVerifications
                        .Include(dv => dv.Request)
                        .Where(dv => dv.Request.CitizenNic == nic)
                        .OrderByDescending(dv => dv.CreatedDate)
                        .Take(10)
                        .Select(dv => new {
                            Date = dv.UpdatedDate ?? dv.CreatedDate,
                            Action = "Document Verification Update",
                            Status = dv.VerificationStatus,
                            Detail = "Verification status changed to " + dv.VerificationStatus
                        })
                        .ToListAsync();

                    var vaultDocs = await _context.DocumentVaultDocuments
                        .Where(vd => vd.NIC == nic)
                        .OrderByDescending(vd => vd.UploadedAt)
                        .Take(10)
                        .Select(vd => new {
                            Date = vd.UploadedAt,
                            Action = "Document Uploaded to Vault",
                            Status = "Completed",
                            Detail = vd.DocumentName + " uploaded successfully"
                        })
                        .ToListAsync();

                    var mergedActivities = reqs.Select(c => new { Date = c.Date, Action = c.Action, Status = c.Status, Detail = c.Detail })
                        .Concat(statusHistories.Select(s => new { Date = s.Date, Action = s.Action, Status = s.Status, Detail = s.Detail }))
                        .Concat(appointments.Select(a => new { Date = a.Date, Action = a.Action, Status = a.Status, Detail = a.Detail }))
                        .Concat(messagesList.Select(m => new { Date = m.Date, Action = m.Action, Status = m.Status, Detail = m.Detail }))
                        .Concat(verifications.Select(v => new { Date = v.Date, Action = v.Action, Status = v.Status, Detail = v.Detail }))
                        .Concat(vaultDocs.Select(vd => new { Date = vd.Date, Action = vd.Action, Status = vd.Status, Detail = vd.Detail }))
                        .OrderByDescending(a => a.Date)
                        .Take(10)
                        .Select(a => new {
                            Date = a.Date.ToString("yyyy-MM-dd HH:mm"),
                            Action = a.Action,
                            Status = a.Status,
                            Detail = a.Detail
                        })
                        .ToList();

                    stats["RecentActivities"] = mergedActivities;

                    var dashboardData = new
                    {
                        Profile = profile,
                        Stats = stats
                    };

                    return Ok(ApiResponse<object>.SuccessResponse(dashboardData, "Citizen dashboard data retrieved successfully."));
                }
                finally
                {
                    if (!wasOpen) await _context.Database.CloseConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error loading citizen dashboard: {ex.Message}"));
            }
        }

        [HttpGet("gn/{nic}")]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> GetGnDashboard(string nic)
        {
            try
            {
                var dbConnection = _context.Database.GetDbConnection();
                var wasOpen = dbConnection.State == ConnectionState.Open;
                if (!wasOpen) await _context.Database.OpenConnectionAsync();

                try
                {
                    // 1. Fetch GN Officer profile and division information
                    var profile = new Dictionary<string, object>();
                    string gnDivisionId = string.Empty;

                    using (var command = dbConnection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                go.[Nic], COALESCE(cm.[FullName], go.[FullName]) AS [FullName],
                                a.[Email], a.[Phone], a.[Status], a.[ProfileImage] AS [Profile],
                                gd.[Id] AS GnDivisionId, gd.[Name] AS GnDivisionName,
                                dd.[Id] AS DsDivisionId, dd.[Name] AS DsDivisionName,
                                (SELECT TOP 1 StartDate FROM [gn_division_assignments] WHERE GnOfficerNic = go.[Nic] ORDER BY StartDate DESC) AS AppointmentDate,
                                (SELECT COUNT(*) FROM [Requests] WHERE [AssignedGnOfficerNic] = go.[Nic] AND [CurrentStatus] = 'Approved') AS TasksCompleted
                            FROM [gn_officers] go
                            INNER JOIN [accounts] a ON go.[Nic] = a.[Nic]
                            INNER JOIN [gn_divisions] gd ON go.[GnDivisionId] = gd.[Id]
                            INNER JOIN [ds_divisions] dd ON gd.[DsDivisionId] = dd.[Id]
                            LEFT JOIN [Citizen_Master] cm ON go.[Nic] = cm.[Nic]
                            WHERE go.[Nic] = @Nic";

                        var param = command.CreateParameter();
                        param.ParameterName = "@Nic";
                        param.Value = nic;
                        command.Parameters.Add(param);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    profile[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                                gnDivisionId = reader["GnDivisionId"].ToString() ?? string.Empty;

                                // Add calculate YearsInService if AppointmentDate is not null
                                if (profile["AppointmentDate"] != null && profile["AppointmentDate"] is DateTime apptDate)
                                {
                                    int years = DateTime.Now.Year - apptDate.Year;
                                    if (DateTime.Now < apptDate.AddYears(years)) years--;
                                    profile["YearsInService"] = years < 0 ? 0 : years;
                                }
                                else
                                {
                                    profile["YearsInService"] = 0;
                                }

                                // Add OfficeAddress default to null for "-"
                                profile["OfficeAddress"] = null;
                            }
                        }
                    }

                    if (profile.Count == 0)
                    {
                        return NotFound(ApiResponse<object>.ErrorResponse("GN Officer profile not found in master records."));
                    }

                    // 2. Fetch division statistics for this GN Division
                    var stats = new Dictionary<string, object>
                    {
                        { "TotalRequests", 0 },
                        { "PendingRequests", 0 },
                        { "TotalCitizens", 0 },
                        { "TotalFamilyCards", 0 },
                        { "RegistryUpdates", 0 },
                        { "DocumentServices", 0 },
                        { "WelfareAssistance", 0 },
                        { "ComplaintsGrievances", 0 },
                        { "GeneralRequests", 0 },
                        { "WeeklyRequestSummary", new int[7] },
                        { "WeeklyRequestSummaryLabels", new string[7] },
                        { "RecentRequests", new List<object>() }
                    };

                    if (!string.IsNullOrEmpty(gnDivisionId))
                    {
                        // Match Request page filters: requests assigned to the logged-in GN Officer
                        int totalRequests = await _context.UnifiedRequests
                            .CountAsync(r => r.AssignedGnOfficerNic == nic);

                        int pendingRequests = await _context.UnifiedRequests
                            .CountAsync(r => r.AssignedGnOfficerNic == nic && 
                                new[] { "pending", "submitted" }
                                .Contains(r.CurrentStatus.ToLower().Trim()));

                        int totalCitizens = await _context.CitizenMasters
                            .CountAsync(cm => cm.FamilyCard.GnDivisionId == gnDivisionId);

                        int totalFamilyCards = await _context.FamilyCards
                            .CountAsync(fc => fc.GnDivisionId == gnDivisionId);

                        stats["TotalRequests"] = totalRequests;
                        stats["PendingRequests"] = pendingRequests;
                        stats["TotalCitizens"] = totalCitizens;
                        stats["TotalFamilyCards"] = totalFamilyCards;

                        // Request Types Breakdown (Registry Updates, Document Services, Welfare & Assistance, Complaints & Grievances, General Requests)
                        var serviceCounts = await _context.UnifiedRequests
                            .Where(r => r.AssignedGnOfficerNic == nic)
                            .GroupBy(r => r.Service.ServiceName)
                            .Select(g => new { ServiceName = g.Key, Count = g.Count() })
                            .ToListAsync();

                        int registryUpdates = 0;
                        int documentServices = 0;
                        int welfareAssistance = 0;
                        int complaintsGrievances = 0;
                        int generalRequests = 0;

                        foreach (var svc in serviceCounts)
                        {
                            var name = svc.ServiceName.ToLower();
                            if (name.Contains("family card") || name.Contains("register to new gn") || name.Contains("voter"))
                            {
                                registryUpdates += svc.Count;
                            }
                            else if (name.Contains("document request") || name.Contains("document verification"))
                            {
                                documentServices += svc.Count;
                            }
                            else if (name.Contains("welfare"))
                            {
                                welfareAssistance += svc.Count;
                            }
                            else if (name.Contains("complaint") || name.Contains("grievance"))
                            {
                                complaintsGrievances += svc.Count;
                            }
                            else
                            {
                                generalRequests += svc.Count;
                            }
                        }

                        stats["RegistryUpdates"] = registryUpdates;
                        stats["DocumentServices"] = documentServices;
                        stats["WelfareAssistance"] = welfareAssistance;
                        stats["ComplaintsGrievances"] = complaintsGrievances;
                        stats["GeneralRequests"] = generalRequests;

                        // Weekly Request Summary for the last seven days
                        TimeZoneInfo slZone;
                        try {
                            slZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
                        } catch {
                            slZone = TimeZoneInfo.CreateCustomTimeZone("SLST", TimeSpan.FromHours(5.5), "Sri Lanka Standard Time", "Sri Lanka Standard Time");
                        }
                        var slNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, slZone);
                        var slToday = slNow.Date;

                        var startDate = slToday.AddDays(-6);
                        var weeklySummary = new int[7];
                        var weeklySummaryLabels = new string[7];

                        var requestDates = await _context.UnifiedRequests
                            .Where(r => r.AssignedGnOfficerNic == nic && r.SubmittedDate >= startDate)
                            .Select(r => r.SubmittedDate)
                            .ToListAsync();

                        for (int i = 0; i < 7; i++)
                        {
                            var targetDate = slToday.AddDays(-6 + i);
                            weeklySummaryLabels[i] = targetDate.ToString("ddd");
                            weeklySummary[i] = requestDates.Count(d => d.Date == targetDate);
                        }
                        stats["WeeklyRequestSummary"] = weeklySummary;
                        stats["WeeklyRequestSummaryLabels"] = weeklySummaryLabels;

                        // Recent Requests (latest 5 requests assigned to this GN Officer, sorted by LastUpdated/SubmittedDate)
                        var recentRequestsList = await _context.UnifiedRequests
                            .Include(r => r.Citizen)
                            .Include(r => r.Category)
                            .Where(r => r.AssignedGnOfficerNic == nic)
                            .OrderByDescending(r => r.LastUpdated)
                            .Take(5)
                            .Select(r => new {
                                Id = r.RequestNumber, // Key must match router link details expectation
                                Name = r.Citizen != null ? r.Citizen.FullName : "Unknown",
                                Type = r.Category != null ? r.Category.CategoryName : "Unknown",
                                Status = r.CurrentStatus,
                                Date = r.SubmittedDate.ToString("yyyy-MM-dd")
                            })
                            .ToListAsync();

                        stats["RecentRequests"] = recentRequestsList;
                    }

                    var dashboardData = new
                    {
                        Profile = profile,
                        Stats = stats
                    };

                    return Ok(ApiResponse<object>.SuccessResponse(dashboardData, "GN Officer dashboard data retrieved successfully."));
                }
                finally
                {
                    if (!wasOpen) await _context.Database.CloseConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error loading GN dashboard: {ex.Message}"));
            }
        }

        [HttpGet("ds/{nic}")]
        [Authorize(Roles = "DSOfficer")]
        public async Task<IActionResult> GetDsDashboard(string nic)
        {
            try
            {
                var dbConnection = _context.Database.GetDbConnection();
                var wasOpen = dbConnection.State == ConnectionState.Open;
                if (!wasOpen) await _context.Database.OpenConnectionAsync();

                try
                {
                    // 1. Fetch DS Officer profile and division information
                    var profile = new Dictionary<string, object>();
                    string dsDivisionId = string.Empty;

                    using (var command = dbConnection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                dso.[Nic], COALESCE(cm.[FullName], dso.[FullName]) AS [FullName],
                                a.[Email], a.[Phone], a.[ProfileImage] AS [Profile],
                                dd.[Id] AS DsDivisionId, dd.[Name] AS DsDivisionName, 
                                dist.[Name] AS District, prov.[Name] AS Province
                            FROM [ds_officers] dso
                            INNER JOIN [accounts] a ON dso.[Nic] = a.[Nic]
                            INNER JOIN [ds_divisions] dd ON dso.[DsDivisionId] = dd.[Id]
                            LEFT JOIN [Districts] dist ON dd.[DistrictId] = dist.[Id]
                            LEFT JOIN [Provinces] prov ON dd.[ProvinceId] = prov.[Id]
                            LEFT JOIN [Citizen_Master] cm ON dso.[Nic] = cm.[Nic]
                            WHERE dso.[Nic] = @Nic";

                        var param = command.CreateParameter();
                        param.ParameterName = "@Nic";
                        param.Value = nic;
                        command.Parameters.Add(param);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    profile[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                                dsDivisionId = reader["DsDivisionId"].ToString() ?? string.Empty;
                            }
                        }
                    }

                    if (profile.Count == 0)
                    {
                        return NotFound(ApiResponse<object>.ErrorResponse("DS Officer profile not found in master records."));
                    }

                    // 2. Fetch administrative statistics for this DS Division
                    var stats = new Dictionary<string, object>
                    {
                        { "TotalGnDivisions", 0 },
                        { "TotalRequests", 0 },
                        { "TotalCitizens", 0 },
                        { "TotalGnOfficers", 0 },
                        { "RecentActivities", new List<object>() }
                    };

                    if (!string.IsNullOrEmpty(dsDivisionId))
                    {
                        int totalGnDivisions = await _context.GnDivisions
                            .CountAsync(gd => gd.DsDivisionId == dsDivisionId);

                        int totalRequests = await _context.UnifiedRequests
                            .CountAsync(r => r.Citizen.GnDivision.DsDivisionId == dsDivisionId);

                        int totalCitizens = await _context.CitizenMasters
                            .CountAsync(cm => cm.FamilyCard.GnDivision.DsDivisionId == dsDivisionId);

                        int totalGnOfficers = await _context.GnOfficers
                            .CountAsync(go => go.GnDivision.DsDivisionId == dsDivisionId);

                        stats["TotalGnDivisions"] = totalGnDivisions;
                        stats["TotalRequests"] = totalRequests;
                        stats["TotalCitizens"] = totalCitizens;
                        stats["TotalGnOfficers"] = totalGnOfficers;

                        // Fetch transfer requests and assignments in division
                        var transfers = await _context.TransferRequests
                            .Include(t => t.GnOfficer)
                            .Include(t => t.CurrentDivision)
                            .Include(t => t.RequestedDivision)
                            .Where(t => t.CurrentDivision.DsDivisionId == dsDivisionId || t.RequestedDivision.DsDivisionId == dsDivisionId)
                            .OrderByDescending(t => t.SubmittedDate)
                            .Take(5)
                            .ToListAsync();

                        var assignments = await _context.GnDivisionAssignments
                            .Include(a => a.GnOfficer)
                            .Include(a => a.GnDivision)
                            .Where(a => a.GnDivision.DsDivisionId == dsDivisionId)
                            .OrderByDescending(a => a.StartDate)
                            .Take(5)
                            .ToListAsync();

                        TimeZoneInfo slZone;
                        try {
                            slZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
                        } catch {
                            slZone = TimeZoneInfo.CreateCustomTimeZone("SLST", TimeSpan.FromHours(5.5), "Sri Lanka Standard Time", "Sri Lanka Standard Time");
                        }
                        var slNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, slZone);
                        var slToday = slNow.Date;

                        Func<DateTime, string> formatRelativeTime = (DateTime date) =>
                        {
                            var targetDate = date.Date;
                            if (targetDate == slToday)
                            {
                                if (date.TimeOfDay != TimeSpan.Zero)
                                {
                                    var diffTime = slNow - date;
                                    if (diffTime.TotalHours < 1)
                                    {
                                        int mins = (int)diffTime.TotalMinutes;
                                        return mins <= 1 ? "Just now" : $"{mins} mins ago";
                                    }
                                    int hours = (int)diffTime.TotalHours;
                                    return hours <= 1 ? "1 hour ago" : $"{hours} hours ago";
                                }
                                return "Today";
                            }
                            if (targetDate == slToday.AddDays(-1))
                            {
                                return "Yesterday";
                            }
                            return date.ToString("yyyy-MM-dd");
                        };

                        var combined = transfers.Select(t => new {
                                Date = t.ReviewedDate.HasValue 
                                    ? new DateTime(t.ReviewedDate.Value.Year, t.ReviewedDate.Value.Month, t.ReviewedDate.Value.Day) 
                                    : new DateTime(t.SubmittedDate.Year, t.SubmittedDate.Month, t.SubmittedDate.Day),
                                Type = "Transfer",
                                Description = t.Status == "Pending"
                                    ? $"GN {t.GnOfficer?.FullName ?? "Officer"} requested a transfer to {t.RequestedDivision?.Name ?? "another division"}."
                                    : $"Transfer request of GN {t.GnOfficer?.FullName ?? "Officer"} to {t.RequestedDivision?.Name ?? "another division"} was {t.Status.ToLower()}.",
                                Status = t.Status == "Pending" ? "pending" : (t.Status == "Approved" ? "completed" : "info")
                            })
                            .Concat(assignments.Select(a => new {
                                Date = a.StartDate,
                                Type = "Registration",
                                Description = $"New GN Officer {a.GnOfficer?.FullName ?? "Officer"} assigned to Division {a.GnDivision?.Name ?? "Division"}.",
                                Status = a.Status == "Active" ? "completed" : "info"
                            }))
                            .OrderByDescending(x => x.Date)
                            .Take(5)
                            .Select(x => new {
                                Id = Guid.NewGuid().ToString(),
                                Type = x.Type,
                                Description = x.Description,
                                Time = formatRelativeTime(x.Date),
                                Status = x.Status
                            })
                            .ToList();

                        stats["RecentActivities"] = combined;
                    }

                    var dashboardData = new
                    {
                        Profile = profile,
                        Stats = stats
                    };

                    return Ok(ApiResponse<object>.SuccessResponse(dashboardData, "DS Officer dashboard data retrieved successfully."));
                }
                finally
                {
                    if (!wasOpen) await _context.Database.CloseConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error loading DS dashboard: {ex.Message}"));
            }
        }

        [HttpGet("ds/{nic}/monitoring")]
        [Authorize(Roles = "DSOfficer")]
        public async Task<IActionResult> GetDsMonitoring(string nic)
        {
            try
            {
                var dsOfficer = await _context.DsOfficers.FirstOrDefaultAsync(dso => dso.Nic == nic);
                if (dsOfficer == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("DS Officer profile not found."));
                }

                string dsDivisionId = dsOfficer.DsDivisionId;

                var gnOfficers = await _context.GnOfficers
                    .Include(go => go.GnDivision)
                    .Where(go => go.GnDivision.DsDivisionId == dsDivisionId)
                    .ToListAsync();

                var monitoringData = new List<object>();

                foreach (var go in gnOfficers)
                {
                    var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Nic == go.Nic);
                    string status = account?.Status ?? "Inactive";
                    string email = account?.Email ?? "No Email";
                    string phone = account?.Phone ?? "No Phone";
                    string lastLogin = account?.LastLogin.HasValue == true 
                        ? account.LastLogin.Value.ToString("yyyy-MM-dd HH:mm") 
                        : "Never";

                    int citizenCount = await _context.CitizenMasters
                        .CountAsync(cm => cm.FamilyCard.GnDivisionId == go.GnDivisionId);

                    int activeRequests = await _context.UnifiedRequests
                        .CountAsync(r => r.AssignedGnOfficerNic == go.Nic && 
                            new[] { "pending", "submitted", "under review", "under_review", "verified", "in progress", "changes requested", "resubmitted", "forwardedtodestinationgn", "returned" }
                            .Contains(r.CurrentStatus.ToLower().Trim()));

                    string workloadStatus = "Optimal";
                    if (activeRequests > 25) workloadStatus = "Overloaded";
                    else if (activeRequests < 5) workloadStatus = "Underutilized";

                    var assignment = await _context.GnDivisionAssignments
                        .Where(a => a.GnOfficerNic == go.Nic)
                        .OrderByDescending(a => a.StartDate)
                        .FirstOrDefaultAsync();

                    string joinedDate = assignment != null 
                        ? assignment.StartDate.ToString("yyyy-MM-dd") 
                        : DateTime.Now.ToString("yyyy-MM-dd");

                    int totalAssigned = await _context.UnifiedRequests
                        .CountAsync(r => r.AssignedGnOfficerNic == go.Nic);

                    int completed = await _context.UnifiedRequests
                        .CountAsync(r => r.AssignedGnOfficerNic == go.Nic && 
                            new[] { "approved", "rejected", "accepted", "revoked" }
                            .Contains(r.CurrentStatus.ToLower().Trim()));

                    int performanceScore = totalAssigned > 0 
                        ? (int)Math.Round(completed * 100.0 / totalAssigned) 
                        : 100;

                    int pendingAppointments = await _context.Appointments
                        .CountAsync(a => a.GnOfficerNic == go.Nic && a.Status == "BOOKED");

                    monitoringData.Add(new
                    {
                        id = go.Nic,
                        name = go.FullName,
                        division = go.GnDivision != null ? go.GnDivision.Name : go.GnDivisionId,
                        status = status,
                        lastLogin = lastLogin,
                        citizenCount = citizenCount,
                        activeRequests = activeRequests,
                        workloadStatus = workloadStatus,
                        email = email,
                        phone = phone,
                        joinedDate = joinedDate,
                        performanceScore = performanceScore,
                        pendingAppointments = pendingAppointments
                    });
                }

                return Ok(ApiResponse<object>.SuccessResponse(monitoringData, "GN Monitoring data retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error loading GN Monitoring: {ex.Message}"));
            }
        }
    }
}
