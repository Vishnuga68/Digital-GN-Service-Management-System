using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EGN_Service_Management.Database;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DistrictsController : ControllerBase
    {
        private readonly EgnDbContext _context;

        public DistrictsController(EgnDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDistricts([FromQuery] int? provinceId)
        {
            var query = _context.Districts.AsQueryable();

            if (provinceId.HasValue)
            {
                query = query.Where(d => d.ProvinceId == provinceId.Value);
            }

            var districts = await query
                .Select(d => new { d.Id, d.Name, d.ProvinceId })
                .OrderBy(d => d.Name)
                .ToListAsync();

            return Ok(districts);
        }
    }
}
