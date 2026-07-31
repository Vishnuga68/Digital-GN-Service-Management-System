using EGN_Service_Management.Database;
using EGN_Service_Management.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
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
                                a.[Email], a.[Phone],
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
                    var stats = new Dictionary<string, int>
                    {
                        { "TotalRequests", 0 },
                        { "PendingRequests", 0 },
                        { "ApprovedRequests", 0 }
                    };

                    using (var command = dbConnection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                COUNT(*) AS TotalRequests,
                                SUM(CASE WHEN [CurrentStatus] = 'Pending' THEN 1 ELSE 0 END) AS PendingRequests,
                                SUM(CASE WHEN [CurrentStatus] = 'Approved' THEN 1 ELSE 0 END) AS ApprovedRequests
                            FROM [Requests]
                            WHERE [CitizenNic] = @Nic";

                        var param = command.CreateParameter();
                        param.ParameterName = "@Nic";
                        param.Value = nic;
                        command.Parameters.Add(param);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                stats["TotalRequests"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                stats["PendingRequests"] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                stats["ApprovedRequests"] = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            }
                        }
                    }

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
                                a.[Email], a.[Phone], a.[Status],
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
                    var stats = new Dictionary<string, int>
                    {
                        { "TotalRequests", 0 },
                        { "PendingRequests", 0 },
                        { "TotalCitizens", 0 },
                        { "TotalFamilyCards", 0 }
                    };

                    if (!string.IsNullOrEmpty(gnDivisionId))
                    {
                        using (var command = dbConnection.CreateCommand())
                        {
                            command.CommandText = @"
                                SELECT 
                                    (SELECT COUNT(*) FROM [Requests] r INNER JOIN [Citizen_Master] cm ON r.[CitizenNic] = cm.[Nic] WHERE cm.[GnDivisionId] = @GnDivId) AS TotalRequests,
                                    (SELECT COUNT(*) FROM [Requests] r INNER JOIN [Citizen_Master] cm ON r.[CitizenNic] = cm.[Nic] WHERE cm.[GnDivisionId] = @GnDivId AND r.[CurrentStatus] = 'Pending') AS PendingRequests,
                                    (SELECT COUNT(*) FROM [Citizen_Master] cm INNER JOIN [family_cards] fc ON cm.[FamilyCardNumber] = fc.[FamilyCardNumber] WHERE fc.[GnDivisionId] = @GnDivId) AS TotalCitizens,
                                    (SELECT COUNT(*) FROM [family_cards] WHERE [GnDivisionId] = @GnDivId) AS TotalFamilyCards";

                            var param = command.CreateParameter();
                            param.ParameterName = "@GnDivId";
                            param.Value = gnDivisionId;
                            command.Parameters.Add(param);

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    stats["TotalRequests"] = reader.GetInt32(0);
                                    stats["PendingRequests"] = reader.GetInt32(1);
                                    stats["TotalCitizens"] = reader.GetInt32(2);
                                    stats["TotalFamilyCards"] = reader.GetInt32(3);
                                }
                            }
                        }
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
                                a.[Email], a.[Phone],
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
                    var stats = new Dictionary<string, int>
                    {
                        { "TotalGnDivisions", 0 },
                        { "TotalRequests", 0 },
                        { "TotalCitizens", 0 },
                        { "TotalGnOfficers", 0 }
                    };

                    if (!string.IsNullOrEmpty(dsDivisionId))
                    {
                        using (var command = dbConnection.CreateCommand())
                        {
                            command.CommandText = @"
                                SELECT 
                                    (SELECT COUNT(*) FROM [gn_divisions] WHERE [DsDivisionId] = @DsDivId) AS TotalGnDivisions,
                                    (SELECT COUNT(*) FROM [Requests] sr INNER JOIN [Citizen_Master] cm ON sr.[CitizenNic] = cm.[Nic] INNER JOIN [gn_divisions] gd ON cm.[GnDivisionId] = gd.[Id] WHERE gd.[DsDivisionId] = @DsDivId) AS TotalRequests,
                                    (SELECT COUNT(*) FROM [Citizen_Master] cm INNER JOIN [family_cards] fc ON cm.[FamilyCardNumber] = fc.[FamilyCardNumber] INNER JOIN [gn_divisions] gd ON fc.[GnDivisionId] = gd.[Id] WHERE gd.[DsDivisionId] = @DsDivId) AS TotalCitizens,
                                    (SELECT COUNT(*) FROM [gn_officers] go INNER JOIN [gn_divisions] gd ON go.[GnDivisionId] = gd.[Id] WHERE gd.[DsDivisionId] = @DsDivId) AS TotalGnOfficers";

                            var param = command.CreateParameter();
                            param.ParameterName = "@DsDivId";
                            param.Value = dsDivisionId;
                            command.Parameters.Add(param);

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    stats["TotalGnDivisions"] = reader.GetInt32(0);
                                    stats["TotalRequests"] = reader.GetInt32(1);
                                    stats["TotalCitizens"] = reader.GetInt32(2);
                                    stats["TotalGnOfficers"] = reader.GetInt32(3);
                                }
                            }
                        }
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
    }
}
