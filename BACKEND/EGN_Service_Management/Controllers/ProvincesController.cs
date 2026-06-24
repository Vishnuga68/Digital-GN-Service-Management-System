using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EGN_Service_Management.Database;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProvincesController : ControllerBase
    {
        private readonly EgnDbContext _context;

        public ProvincesController(EgnDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProvinces()
        {
            var provinces = await _context.Provinces
                .Select(p => new { p.Id, p.Name })
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(provinces);
        }
    }
}
