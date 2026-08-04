using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "GNOfficer")]
    public class VoterRegistryController : ControllerBase
    {
        private readonly EgnDbContext _context;

        public VoterRegistryController(EgnDbContext context)
        {
            _context = context;
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

        [HttpGet("division/{gnDivisionId}")]
        public async Task<IActionResult> GetVotersByDivision(string gnDivisionId)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
            if (officer == null)
            {
                return Forbid();
            }

            var targetDivisionId = officer.GnDivisionId;

            var voters = await _context.VoterRegistries
                .Include(vr => vr.Citizen)
                .Where(vr => vr.Citizen != null && vr.Citizen.GnDivisionId == targetDivisionId)
                .Select(vr => new
                {
                    vr.Id,
                    vr.CitizenNic,
                    CitizenName = vr.Citizen != null ? vr.Citizen.FullName : "Unknown",
                    RegisteredDate = vr.RegisteredDate,
                    vr.RegisteredByGnOfficer,
                    vr.IsExported
                })
                .OrderByDescending(vr => vr.RegisteredDate)
                .ToListAsync();

            return Ok(new { success = true, data = voters });
        }

        [HttpPost("division/{gnDivisionId}/export")]
        public async Task<IActionResult> ExportVoters(string gnDivisionId)
        {
            var gnNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(gnNic)) return Unauthorized();

            var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == gnNic);
            if (officer == null || officer.GnDivisionId != gnDivisionId)
            {
                return Forbid();
            }

            var nonExported = await _context.VoterRegistries
                .Include(vr => vr.Citizen)
                .Where(vr => vr.Citizen != null && vr.Citizen.GnDivisionId == gnDivisionId && !vr.IsExported)
                .ToListAsync();

            if (nonExported.Count == 0)
            {
                return BadRequest(new { success = false, message = "No new approved voter records to export." });
            }

            var builder = new StringBuilder();
            builder.AppendLine("CitizenNic,FullName,RegisteredDate,RegisteredByGnOfficer");
            foreach (var voter in nonExported)
            {
                var name = voter.Citizen?.FullName ?? "Unknown";
                if (name.Contains(",")) name = $"\"{name}\"";
                builder.AppendLine($"{voter.CitizenNic},{name},{voter.RegisteredDate:yyyy-MM-dd HH:mm:ss},{voter.RegisteredByGnOfficer}");
                
                voter.IsExported = true;
            }

            await _context.SaveChangesAsync();

            var csvBytes = Encoding.UTF8.GetBytes(builder.ToString());
            return File(csvBytes, "text/csv", $"voters_export_{gnDivisionId}_{GetSriLankanTime():yyyyMMddHHmmss}.csv");
        }
    }
}
