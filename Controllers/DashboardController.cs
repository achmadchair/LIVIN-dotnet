using Livin.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Livin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserSiteId()
        {
            var siteIdClaim = User.FindFirst("SiteId")?.Value;
            if (int.TryParse(siteIdClaim, out int siteId))
            {
                return siteId;
            }
            return -1;
        }

        [HttpGet("chart")]
        public async Task<IActionResult> GetChartData()
        {
            int userSiteId = GetUserSiteId();
            var query = _context.Sites.AsQueryable();
            if (userSiteId > 0)
            {
                query = query.Where(s => s.Id == userSiteId);
            }
            var sites = await query.OrderBy(s => s.Id).ToListAsync();

            var labels = new List<string>();
            var scheduled = new List<int>();
            var actual = new List<int>();

            foreach (var site in sites)
            {
                labels.Add(site.Name);

                // Scheduled: How many equipments in this site have a NextInspectionDate relative to anything (or just total count of equipments in site with a date)
                var scheduledCount = await _context.Equipments
                    .CountAsync(e => e.SiteId == site.Id && e.NextInspectionDate != null);
                
                // Actual: How many unique equipments in this site appear in the InspectionRecords
                var actualCount = await _context.InspectionRecords
                    .Where(r => r.SiteId == site.Id)
                    .Select(r => r.EquipmentId)
                    .Distinct()
                    .CountAsync();

                scheduled.Add(scheduledCount);
                actual.Add(actualCount);
            }

            return Ok(new
            {
                labels,
                scheduled,
                actual
            });
        }

        [HttpGet("inspected")]
        public async Task<IActionResult> GetInspectedEquipment()
        {
            int userSiteId = GetUserSiteId();
            var query = _context.InspectionRecords.AsQueryable();
            if (userSiteId > 0)
            {
                query = query.Where(r => r.SiteId == userSiteId);
            }

            // Returns the most recent distinct equipments inspected
            var recentRecords = await query
                .Include(r => r.Equipment)
                .Include(r => r.Site)
                .OrderByDescending(r => r.InspectionDate)
                .Take(20) // take top 20 recent records
                .Select(r => new
                {
                    r.Id,
                    EquipmentName = r.Equipment != null ? r.Equipment.Name : "Unknown",
                    HacCode = r.Equipment != null ? r.Equipment.HACCode : "-",
                    SiteName = r.Site != null ? r.Site.Name : "-",
                    r.InspectionDate,
                    Status = r.Details.Any(d => !d.IsPassed) ? "Needs Review" : "Passed"
                })
                .ToListAsync();

            return Ok(recentRecords);
        }

        [HttpGet("frequent")]
        public async Task<IActionResult> GetFrequentlyInspected()
        {
            int userSiteId = GetUserSiteId();
            var query = _context.InspectionRecords.AsQueryable();
            if (userSiteId > 0)
            {
                query = query.Where(r => r.SiteId == userSiteId);
            }

            // Group by Equipment to find the most frequently inspected
            var frequent = await query
                .Include(r => r.Equipment)
                .Include(r => r.Site)
                .GroupBy(r => new { 
                    EqId = r.EquipmentId, 
                    EqName = r.Equipment != null ? r.Equipment.Name : "Unknown", 
                    Hac = r.Equipment != null ? r.Equipment.HACCode : "-", 
                    SiteName = r.Site != null ? r.Site.Name : "-" 
                })
                .Select(g => new
                {
                    EquipmentName = g.Key.EqName,
                    HacCode = g.Key.Hac,
                    SiteName = g.Key.SiteName,
                    InspectionCount = g.Count(),
                    LastInspection = g.Max(r => r.InspectionDate)
                })
                .OrderByDescending(x => x.InspectionCount)
                .Take(10)
                .ToListAsync();

            return Ok(frequent);
        }
    }
}
