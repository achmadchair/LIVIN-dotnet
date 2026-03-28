using Livin.Api.Data;
using Livin.Api.DTOs;
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
    public class InspectionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InspectionController(ApplicationDbContext context)
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

        private int GetUserId()
        {
            // In a real app, user ID would be in claims
            // For now, we'll look up by username from Name claim
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            return user?.Id ?? -1;
        }

        [HttpPost("submit")]
        public async Task<ActionResult> SubmitInspection(InspectionSubmissionRequest request)
        {
            int siteId = GetUserSiteId();
            int userId = GetUserId();

            var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
            
            if (equipment == null)
            {
                return NotFound("Equipment not found.");
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var inspectorCategory = User.FindFirst("InspectorCategory")?.Value ?? string.Empty;

            if (userRole != "Leader" && !string.IsNullOrEmpty(inspectorCategory) && !string.IsNullOrEmpty(equipment.Type))
            {
                if (!equipment.Type.Equals(inspectorCategory, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Access denied. You cannot inspect equipment of this category.");
                }
            }

            var record = new InspectionRecord
            {
                EquipmentId = request.EquipmentId,
                InspectorId = userId,
                SiteId = siteId,
                InspectionDate = DateTime.UtcNow,
                Type = request.Type,
                Details = request.Details.Select(d => new InspectionDetail
                {
                    InspectionTaskId = d.InspectionTaskId,
                    TaskStandardId = d.TaskStandardId,
                    IsPassed = d.IsPassed,
                    Remarks = d.Remarks,
                    SelectedTask = d.SelectedTask,
                    FollowUpAction = d.FollowUpAction
                }).ToList()
            };

            _context.InspectionRecords.Add(record);
            await _context.SaveChangesAsync();
            
            // Check if maintenance is required
            var needsMaintenance = request.Details
                .Where(d => d.FollowUpAction.Equals("Maintenance", StringComparison.OrdinalIgnoreCase) 
                         || d.FollowUpAction.Equals("Repair", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach(var item in needsMaintenance)
            {
                _context.MaintenanceRecords.Add(new MaintenanceRecord
                {
                    EquipmentId = request.EquipmentId,
                    InspectionRecordId = record.Id,
                    Remarks = $"Triggered from inspection. Standard ID: {item.TaskStandardId}. Selected task: {item.SelectedTask}."
                });
            }
            if (needsMaintenance.Any())
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Inspection submitted successfully", RecordId = record.Id });
        }
    }
}
