using Livin.Api.Data;
using Livin.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Livin.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceController(ApplicationDbContext context)
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

        [HttpGet("open")]
        public async Task<ActionResult> GetOpenMaintenance()
        {
            int siteId = GetUserSiteId();

            var records = await _context.MaintenanceRecords
                .Include(m => m.Equipment)
                .Where(m => (m.Equipment.SiteId == siteId || siteId == -1) && m.Status == "Open")
                .Select(m => new
                {
                    id = m.Id,
                    equipmentName = m.Equipment.Name,
                    hacCode = m.Equipment.HACCode,
                    remarks = m.Remarks,
                    requestedDate = m.RequestedDate
                })
                .ToListAsync();

            return Ok(records);
        }

        [HttpPost("complete/{id}")]
        public async Task<ActionResult> CompleteMaintenance(int id, [FromBody] CompleteMaintenanceRequest request)
        {
            var record = await _context.MaintenanceRecords.FindAsync(id);
            if (record == null) return NotFound();

            record.Status = "Completed";
            record.CompletedDate = DateTime.UtcNow;
            record.Technician = request.Technician;
            record.CompletionNotes = request.CompletionNotes;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Maintenance completed successfully" });
        }
    }

    public class CompleteMaintenanceRequest
    {
        public string Technician { get; set; } = string.Empty;
        public string CompletionNotes { get; set; } = string.Empty;
    }
}
