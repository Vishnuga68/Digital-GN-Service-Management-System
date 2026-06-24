using EGN_Service_Management.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly EgnDbContext _context;

        public ServicesController(EgnDbContext context)
        {
            _context = context;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveServices()
        {
            var services = await _context.UnifiedServices
                .Select(s => new { s.ServiceId, s.ServiceName })
                .ToListAsync();

            return Ok(services);
        }
    }
}
