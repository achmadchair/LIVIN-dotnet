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
    public class EquipmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EquipmentController(ApplicationDbContext context)
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

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        private string GetUserInspectorCategory()
        {
            return User.FindFirst("InspectorCategory")?.Value ?? string.Empty;
        }

        [HttpGet("scan/{hacCode}")]
        public async Task<ActionResult<object>> ScanEquipment(string hacCode)
        {
            var equipment = await _context.Equipments
                .Include(e => e.Parts)
                    .ThenInclude(p => p.Tasks)
                        .ThenInclude(t => t.Standards)
                .FirstOrDefaultAsync(e => e.HACCode == hacCode);

            if (equipment == null)
            {
                return NotFound("Equipment not found.");
            }

            int userSiteId = GetUserSiteId();
            if (userSiteId > 0 && equipment.SiteId != userSiteId)
            {
                return BadRequest("Access denied. Equipment belongs to a different site.");
            }

            var userRole = GetUserRole();
            var inspectorCategory = GetUserInspectorCategory();
            if (userRole != "Leader" && !string.IsNullOrEmpty(inspectorCategory))
            {
                var catLower = inspectorCategory.ToLower();
                if (catLower == "safety")
                {
                    if (string.IsNullOrEmpty(equipment.Type) || !equipment.Type.Equals("safety", StringComparison.OrdinalIgnoreCase))
                        return BadRequest("Access denied. Equipment category mismatch.");
                }
                else 
                {
                    if (!string.IsNullOrEmpty(equipment.Type) && !equipment.Type.Equals(inspectorCategory, StringComparison.OrdinalIgnoreCase))
                        return BadRequest("Access denied. Equipment category mismatch.");
                }
            }

            // Schedule Validation Rule
            if (equipment.NextInspectionDate.HasValue && equipment.NextInspectionDate.Value.Date > DateTime.UtcNow.Date)
            {
                return BadRequest("Equipment ini dijadwalkan untuk inspeksi pada " + equipment.NextInspectionDate.Value.ToString("dd-MM-yyyy") + ". Belum bisa diinspeksi hari ini.");
            }

            var result = new
            {
                id = equipment.Id,
                hacCode = equipment.HACCode,
                name = equipment.Name,
                type = equipment.Type,
                parts = equipment.Parts.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    type = p.Type,
                    group = p.Group,
                    tasks = p.Tasks.Select(t => new
                    {
                        id = t.Id,
                        name = t.Description,
                        type = t.Type,
                        group = t.Group,
                        standards = t.Standards.Select(s => new
                        {
                            id = s.Id,
                            standardText = s.StandardText
                        })
                    })
                })
            };

            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetGallery()
        {
            int siteId = GetUserSiteId();
            var query = _context.Equipments.Include(e => e.Site).AsQueryable();
            if (siteId > 0)
            {
                query = query.Where(e => e.SiteId == siteId);
            }

            var userRole = GetUserRole();
            var inspectorCategory = GetUserInspectorCategory();
            if (userRole != "Leader" && !string.IsNullOrEmpty(inspectorCategory))
            {
                var catLower = inspectorCategory.ToLower();
                if (catLower == "safety")
                {
                    query = query.Where(e => e.Type != null && e.Type.ToLower() == "safety");
                }
                else
                {
                    query = query.Where(e => string.IsNullOrEmpty(e.Type) || e.Type.ToLower() == catLower);
                }
            }

            var equipments = await query
                .Select(e => new { id = e.Id, hacCode = e.HACCode, name = e.Name, type = e.Type, group = e.Group, siteName = e.Site.Name })
                .ToListAsync();

            return Ok(equipments);
        }

        [HttpGet("template/{type}")]
        [AllowAnonymous] 
        public IActionResult DownloadTemplate(string type)
        {
            string csv = type.ToLower() switch
            {
                "equipment" => "Kode Hac,site,Nama equipment,Type,Group",
                "part" => "Kode Hac,Nama Part,Type,Group",
                "task" => "Kode Hac,Part,Type,Group,Nama Task",
                "standard" => "Kode Hac,Part,Type,Group,Nama Task,Nama Standard",
                "schedule" => "site,kodehac,nama equipment,tanggal (YYYY-MM-DD)",
                _ => null
            };

            if (csv == null) return BadRequest("Invalid template type");
            
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{type}_template.csv");
        }

        [Authorize(Roles = "Leader")]
        [HttpPost("upload/{type}")]
        public async Task<IActionResult> UploadCSV(string type, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            if (lines.Count > 0 && lines[0].ToLower().Contains("kode") || lines[0].ToLower().Contains("site"))
            {
                lines.RemoveAt(0); 
            }
            
            int added = 0;
            int skippedCount = 0;
            var errors = new List<string>();

            foreach (var line in lines)
            {
                var delimiter = line.Contains(';') ? ';' : (line.Contains('\t') ? '\t' : ',');
                var cols = line.Split(delimiter).Select(c => c.Trim()).ToArray();
                if (cols.Length < 2) continue;

                if (type.Equals("equipment", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: "Kode Hac,site,Nama equipment,Type,Group"
                    var hac = cols[0];
                    var siteName = cols.Length > 1 ? cols[1] : "";
                    var name = cols.Length > 2 ? cols[2] : "";
                    var typeEq = cols.Length > 3 ? cols[3] : "";
                    var groupEq = cols.Length > 4 ? cols[4] : "";

                    if (string.IsNullOrEmpty(siteName) || string.IsNullOrEmpty(hac)) continue;

                    var site = await _context.Sites.FirstOrDefaultAsync(s => s.Name.ToLower() == siteName.ToLower());
                    
                    int userSiteId = GetUserSiteId();
                    if (userSiteId > 0)
                    {
                        if (site != null && site.Id != userSiteId)
                            return BadRequest($"Row {added + skippedCount + 1}: You can only upload data for your assigned site.");
                        else if (site == null)
                            return BadRequest($"Row {added + skippedCount + 1}: Site not found or you are not authorized to create new sites.");
                    }

                    if (site == null)
                    {
                        site = new Site { Name = siteName };
                        _context.Sites.Add(site);
                        await _context.SaveChangesAsync();
                    }

                    var eq = await _context.Equipments.FirstOrDefaultAsync(e => e.HACCode == hac);
                    if (eq == null)
                    {
                        eq = new Equipment { HACCode = hac, Name = name, Type = typeEq, Group = groupEq, SiteId = site.Id };
                        _context.Equipments.Add(eq);
                        await _context.SaveChangesAsync();
                        added++;
                    }
                }
                else if (type.Equals("part", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: "Kode Hac,Nama Part,Type,group"
                    var hac = cols[0];
                    var partName = cols.Length > 1 ? cols[1] : "";
                    var typePart = cols.Length > 2 ? cols[2] : "";
                    var groupPart = cols.Length > 3 ? cols[3] : "";

                    if (cols.All(string.IsNullOrWhiteSpace)) continue;

                    List<Equipment> targetEqs = new List<Equipment>();
                    if (!string.IsNullOrWhiteSpace(hac)) {
                        targetEqs = await _context.Equipments.Where(e => e.HACCode == hac).ToListAsync();
                    } else if (typePart.Equals("Safety", StringComparison.OrdinalIgnoreCase)) {
                        targetEqs = await _context.Equipments.Where(e => e.Type.ToLower() == "safety").ToListAsync();
                    } else {
                        skippedCount++;
                        errors.Add($"Row: Kode Hac is empty and Type is not Safety.");
                        continue;
                    }

                    if (!targetEqs.Any()) {
                        skippedCount++;
                        errors.Add($"Row: No matching equipments found for HAC '{hac}' or Type '{typePart}'.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(partName)) {
                        skippedCount++;
                        errors.Add($"Row: Part name missing.");
                        continue;
                    }

                    bool anyAdded = false;
                    foreach (var eq in targetEqs)
                    {
                        if (!await _context.Parts.AnyAsync(p => p.EquipmentId == eq.Id && p.Name == partName))
                        {
                            _context.Parts.Add(new Part { HACCode = eq.HACCode, Name = partName, Type = typePart, Group = groupPart, EquipmentId = eq.Id, SiteId = eq.SiteId });
                            anyAdded = true;
                        }
                    }

                    if (anyAdded) {
                        await _context.SaveChangesAsync();
                        added++;
                    } else {
                        skippedCount++;
                    }
                }
                else if (type.Equals("task", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: "Kode Hac,Part,Type,Group,Nama Task"
                    var hac = cols[0]; // Ignored for Task
                    var partName = cols.Length > 1 ? cols[1].Trim() : "";
                    var typeTask = cols.Length > 2 ? cols[2].Trim() : "";
                    var groupTask = cols.Length > 3 ? cols[3].Trim() : "";
                    var taskName = cols.Length > 4 ? cols[4].Trim() : "";

                    if (cols.All(string.IsNullOrWhiteSpace)) continue;

                    if (string.IsNullOrWhiteSpace(partName)) {
                        skippedCount++;
                        errors.Add($"Row: Part name is required. Data: {string.Join(",", cols)}");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(taskName)) {
                        skippedCount++;
                        errors.Add($"Row: Task Name is empty.");
                        continue;
                    }

                    var query = _context.Parts.Where(p => p.Name == partName && p.Group == groupTask);
                    int userSiteId = GetUserSiteId();
                    if (userSiteId > 0) {
                        query = query.Where(p => p.SiteId == userSiteId);
                    }
                    var targetParts = await query.ToListAsync();

                    if (!targetParts.Any()) {
                        skippedCount++;
                        errors.Add($"Row: No parts found matching Name '{partName}' and Group '{groupTask}'.");
                        continue;
                    }

                    bool anyAdded = false;
                    foreach (var part in targetParts)
                    {
                        if (!await _context.InspectionTasks.AnyAsync(t => t.PartId == part.Id && t.Description == taskName && t.Group == groupTask))
                        {
                            _context.InspectionTasks.Add(new InspectionTask 
                            { 
                                Description = taskName, 
                                PartId = part.Id, 
                                Type = typeTask, 
                                Group = groupTask,
                                HACCode = part.HACCode, // Sync from part
                                PartName = part.Name
                            });
                            anyAdded = true;
                        }
                    }

                    if (anyAdded) {
                        await _context.SaveChangesAsync();
                        added++;
                    } else {
                        skippedCount++;
                        errors.Add($"Row: Task '{taskName}' already exists for matching parts.");
                    }
                }
                else if (type.Equals("standard", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: "Kode Hac,Part,Type,Group,Nama Task,Nama Standard"
                    var hac = cols[0]; // Ignored for Standard
                    var partName = cols.Length > 1 ? cols[1].Trim() : "";
                    var typeStd = cols.Length > 2 ? cols[2].Trim() : "";
                    var groupStd = cols.Length > 3 ? cols[3].Trim() : "";
                    var taskNameStr = cols.Length > 4 ? cols[4].Trim() : "";
                    var standardTxt = cols.Length > 5 ? cols[5].Trim() : "";

                    if (cols.All(string.IsNullOrWhiteSpace)) continue;

                    if (string.IsNullOrWhiteSpace(partName) || string.IsNullOrWhiteSpace(taskNameStr)) {
                        skippedCount++;
                        errors.Add($"Row: Part Name and Task Name are required.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(standardTxt)) {
                        skippedCount++;
                        errors.Add($"Row: Standard Text is empty.");
                        continue;
                    }

                    var query = _context.InspectionTasks.Where(t => t.PartName == partName && t.Group == groupStd && t.Description == taskNameStr);
                    int userSiteId = GetUserSiteId();
                    if (userSiteId > 0) {
                        query = query.Where(t => t.Part.SiteId == userSiteId);
                    }
                    var targetTasks = await query.Include(t => t.Part).ToListAsync();

                    if (!targetTasks.Any()) {
                        skippedCount++;
                        errors.Add($"Row: No Tasks found matching Part '{partName}', Group '{groupStd}', Task '{taskNameStr}'.");
                        continue;
                    }

                    bool anyAdded = false;
                    foreach (var task in targetTasks)
                    {
                        if (!await _context.TaskStandards.AnyAsync(s => s.InspectionTaskId == task.Id && s.StandardText == standardTxt))
                        {
                            _context.TaskStandards.Add(new TaskStandard 
                            { 
                                StandardText = standardTxt, 
                                InspectionTaskId = task.Id, 
                                Type = typeStd, 
                                Group = groupStd,
                                HACCode = task.HACCode, // Sync from task
                                PartName = task.PartName,
                                TaskName = task.Description
                            });
                            anyAdded = true;
                        }
                    }

                    if (anyAdded) {
                        await _context.SaveChangesAsync();
                        added++;
                    } else {
                        skippedCount++;
                        errors.Add($"Row: Standard '{standardTxt}' already exists for matching tasks.");
                    }
                }
                else if (type.Equals("schedule", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: site, kodehac, nama equipment, tanggal (YYYY-MM-DD)
                    var siteName = cols[0];
                    var hac = cols.Length > 1 ? cols[1] : "";
                    var name = cols.Length > 2 ? cols[2] : "";
                    var dateStr = cols.Length > 3 ? cols[3] : "";
                    
                    if (string.IsNullOrEmpty(siteName) || string.IsNullOrEmpty(hac)) continue;

                    var site = await _context.Sites.FirstOrDefaultAsync(s => s.Name.ToLower() == siteName.ToLower());
                    if (site == null)
                    {
                        site = new Site { Name = siteName };
                        _context.Sites.Add(site);
                        await _context.SaveChangesAsync();
                    }

                    var eq = await _context.Equipments.FirstOrDefaultAsync(e => e.HACCode == hac);
                    if (eq == null)
                    {
                        eq = new Equipment { HACCode = hac, Name = name, SiteId = site.Id };
                        _context.Equipments.Add(eq);
                        await _context.SaveChangesAsync();
                        added++;
                    }

                    if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                    {
                        eq.NextInspectionDate = parsedDate;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            await _context.SaveChangesAsync();
            string msg = $"Successfully imported {added} records. (Ignored: {skippedCount})";
            if (errors.Any()) {
                msg += $" Errors: {string.Join(" | ", errors.Take(3))}";
            }
            return Ok(new { message = msg });
        }
    }
}
