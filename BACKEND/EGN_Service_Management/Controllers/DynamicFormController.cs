using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EGN_Service_Management.Database;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicFormController : ControllerBase
    {
        private readonly EgnDbContext _context;

        public DynamicFormController(EgnDbContext context)
        {
            _context = context;
        }

        // 1. Load Services dynamically grouped by Headings
        [HttpGet("headings-with-services")]
        public async Task<IActionResult> GetHeadingsWithServices()
        {
            var headings = await _context.ServiceParentHeadings
                .Include(h => h.Services)
                    .ThenInclude(s => s.Categories)
                .Select(h => new
                {
                    h.ParentHeadingId,
                    h.ParentHeadingName,
                    h.Icon,
                    h.Description,
                    Services = h.Services.Select(s => new
                    {
                        s.ServiceId,
                        ServiceName = s.ServiceName,
                        s.Icon,
                        s.Description,
                        Categories = s.Categories.Select(c => new
                        {
                            c.CategoryId,
                            c.CategoryName,
                            c.GuidelineText
                        }).ToList()
                    }).ToList()
                })
                .ToListAsync();

            return Ok(headings);
        }

        // 2. Load Form from DB (derived from UI)
        [HttpGet("schema/{categoryId}")]
        public async Task<IActionResult> GetFormSchema(int categoryId)
        {
            try {
                var schemaFields = await _context.ServiceFormSchemas
                    .Where(s => s.CategoryId == categoryId && s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new
                    {
                        s.Id,
                        s.FieldName,
                        s.FieldLabel,
                        s.FieldType,
                        s.DropdownOptions,
                        s.IsRequired,
                        s.ValidationRules,
                        s.DisplayOrder,
                        s.FormSection
                    })
                    .ToListAsync();

                return Ok(schemaFields);
            } catch (System.Exception ex) {
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace, inner = ex.InnerException?.Message });
            }
        }

        // 3. Auto-fill Profile Data
        [HttpGet("profile/{nic}")]
        public async Task<IActionResult> GetCitizenProfile(string nic)
        {
            var citizen = await _context.CitizenMasters
                .Where(c => c.Nic == nic)
                .Select(c => new
                {
                    c.Nic,
                    c.FullName,
                    c.Dob,
                    c.FamilyCardNumber
                })
                .FirstOrDefaultAsync();

            if (citizen == null) return NotFound("Citizen not found");

            var account = await _context.Accounts
                .Where(a => a.Nic == nic)
                .Select(a => new { a.Email, a.Phone })
                .FirstOrDefaultAsync();

            var familyCard = await _context.FamilyCards
                .Where(f => f.FamilyCardNumber == citizen.FamilyCardNumber)
                .Select(f => new { f.Address })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                fullName = citizen.FullName,
                nic = citizen.Nic,
                phone = account?.Phone,
                email = account?.Email,
                familyCardNumber = citizen.FamilyCardNumber,
                address = familyCard?.Address,
                dob = citizen.Dob
            });
        }
        // 4. Submit Dynamic Form
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitDynamicForm([FromBody] DynamicFormSubmitDTO payload)
        {
            if (string.IsNullOrEmpty(payload.CitizenNic))
                return BadRequest("Citizen NIC is required");

            // Find GN Division of the citizen
            var citizen = await _context.CitizenMasters
                .Where(c => c.Nic == payload.CitizenNic)
                .Select(c => new { c.GnDivisionId })
                .FirstOrDefaultAsync();

            string? gnDivision = citizen?.GnDivisionId;
            string? assignedOfficer = null;

            if (!string.IsNullOrEmpty(gnDivision))
            {
                assignedOfficer = await _context.GnOfficers
                    .Where(g => g.GnDivisionId == gnDivision)
                    .Select(g => g.Nic)
                    .FirstOrDefaultAsync();
            }

            var requestNumber = $"REQ-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

            var request = new Entity.UnifiedRequest
            {
                CitizenNic = payload.CitizenNic,
                ServiceId = payload.ServiceId,
                CategoryId = payload.CategoryId,
                CurrentStatus = "Pending",
                SubmittedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                AssignedGnOfficerNic = assignedOfficer,
                GnDivision = gnDivision,
                RequestNumber = requestNumber
            };

            _context.UnifiedRequests.Add(request);
            await _context.SaveChangesAsync();

            var requestData = new Entity.RequestData
            {
                RequestId = request.RequestId,
                JsonData = payload.DataJson,
                CreatedDate = DateTime.UtcNow
            };

            _context.RequestDataEntries.Add(requestData);

            var statusHistory = new Entity.RequestStatusHistory
            {
                RequestId = request.RequestId,
                Status = "Pending",
                ChangedDate = DateTime.UtcNow,
                ChangedBy = payload.CitizenNic, // Or the actual citizen's NIC as string
                Remarks = "Request submitted via Citizen Portal"
            };

            _context.RequestStatusHistories.Add(statusHistory);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Request submitted successfully", requestId = request.RequestNumber });
        }
    }

    public class DynamicFormSubmitDTO
    {
        public int ServiceId { get; set; }
        public int CategoryId { get; set; }
        public string CitizenNic { get; set; }
        public string DataJson { get; set; }
    }
}
