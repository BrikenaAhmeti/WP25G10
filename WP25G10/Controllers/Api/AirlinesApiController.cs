using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;
using WP25G10.Models.Dto;

namespace WP25G10.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AirlinesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AirlinesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/airlines?search=xx
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AirlineDto>>> GetAirlines([FromQuery] string? search = null)
        {
            var q = _context.Airlines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(a =>
                    a.Name.ToLower().Contains(s) ||
                    a.Code.ToLower().Contains(s));
            }

            var result = await q
                .OrderBy(a => a.Name)
                .Select(a => new AirlineDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Code = a.Code,
                    IsActive = a.IsActive
                })
                .ToListAsync();

            return Ok(result);
        }
    }
}
