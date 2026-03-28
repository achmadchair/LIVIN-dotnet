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
                .Include(e => e.Tasks)
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
            if (userRole != "Leader" && !string.IsNullOrEmpty(inspectorCategory) && !string.IsNullOrEmpty(equipment.Type))
            {
                if (!equipment.Type.Equals(inspectorCategory, StringComparison.OrdinalIgnoreCase))
                {
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
                tasks = equipment.Tasks.Select(t => new
                {
                    id = t.Id,
                    description = t.Description,
                    standards = t.Standards.Select(s => new
                    {
                        id = s.Id,
                        standardText = s.StandardText
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
                query = query.Where(e => string.IsNullOrEmpty(e.Type) || e.Type.ToLower() == catLower);
            }

            // Return equipment. Exposing siteName and explicit lowercase property names for frontend compatibility.
            var equipments = await query
                .Select(e => new { id = e.Id, hacCode = e.HACCode, name = e.Name, type = e.Type, siteName = e.Site.Name })
                .ToListAsync();

            return Ok(equipments);
        }

        [HttpGet("template/{type}")]
        [AllowAnonymous] 
        public IActionResult DownloadTemplate(string type)
        {
            string csv = type.ToLower() switch
            {
                "equipment" => "Kode Hac,site,Nama equipment,Type,Part,Standard",
                "schedule" => "site,kodehac,nama equipment,part,tanggal (YYYY-MM-DD)",
                "task" => "Kode hac,nama equipment,part",
                "standard" => "kode hac,nama equipment,part,standar",
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
            
            // Smarter header skipping
            if (lines.Count > 0 && lines[0].ToLower().Contains("kode"))
            {
                lines.RemoveAt(0); 
            }
            
            int added = 0;
            int skippedCount = 0;

            foreach (var line in lines)
            {
                var delimiter = line.Contains(';') ? ';' : ',';
                var cols = line.Split(delimiter).Select(c => c.Trim()).ToArray();
                if (cols.Length < 3) continue;

                if (type.Equals("equipment", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: Kode Hac,site,Nama equipment,Type,Part,Standard
                    var hac = cols[0];
                    var siteName = cols.Length > 1 ? cols[1] : "";
                    var name = cols.Length > 2 ? cols[2] : "";
                    var typeEq = cols.Length > 3 ? cols[3] : "";
                    var partDesc = cols.Length > 4 ? cols[4] : "";
                    var standardTxt = cols.Length > 5 ? cols[5] : "";

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
                        eq = new Equipment { HACCode = hac, Name = name, Type = typeEq, SiteId = site.Id };
                        _context.Equipments.Add(eq);
                        await _context.SaveChangesAsync();
                        added++;
                    }

                    if (!string.IsNullOrEmpty(partDesc))
                    {
                        var task = await _context.InspectionTasks.FirstOrDefaultAsync(t => t.EquipmentId == eq.Id && t.Description == partDesc);
                        if (task == null)
                        {
                            task = new InspectionTask { Description = partDesc, EquipmentId = eq.Id };
                            _context.InspectionTasks.Add(task);
                            await _context.SaveChangesAsync();
                            added++;
                        }

                        if (!string.IsNullOrEmpty(standardTxt))
                        {
                            if (!await _context.TaskStandards.AnyAsync(s => s.InspectionTaskId == task.Id && s.StandardText == standardTxt))
                            {
                                _context.TaskStandards.Add(new TaskStandard { StandardText = standardTxt, InspectionTaskId = task.Id });
                                added++;
                            }
                        }
                    }
                }
                else if (type.Equals("task", StringComparison.OrdinalIgnoreCase))
                {
                    var hac = cols[0];
                    var taskDesc = cols[2];

                    var eq = await _context.Equipments.FirstOrDefaultAsync(e => e.HACCode == hac);
                    if (eq != null)
                    {
                        if (!await _context.InspectionTasks.AnyAsync(t => t.EquipmentId == eq.Id && t.Description == taskDesc))
                        {
                            _context.InspectionTasks.Add(new InspectionTask { Description = taskDesc, EquipmentId = eq.Id });
                            added++;
                        }
                        else { skippedCount++; }
                    }
                    else { skippedCount++; }
                }
                else if (type.Equals("schedule", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: site, kodehac, nama equipment, part, tanggal (YYYY-MM-DD)
                    var siteName = cols[0];
                    var hac = cols.Length > 1 ? cols[1] : "";
                    var name = cols.Length > 2 ? cols[2] : "";
                    var partDesc = cols.Length > 3 ? cols[3] : "";
                    
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

                    // Create Part if it doesn't exist
                    if (!string.IsNullOrEmpty(partDesc))
                    {
                        var task = await _context.InspectionTasks.FirstOrDefaultAsync(t => t.EquipmentId == eq.Id && t.Description == partDesc);
                        if (task == null)
                        {
                            task = new InspectionTask { Description = partDesc, EquipmentId = eq.Id };
                            _context.InspectionTasks.Add(task);
                            await _context.SaveChangesAsync();
                            added++;
                        }
                    }
                    
                    // Parse Date if provided
                    if (cols.Length > 4 && DateTime.TryParse(cols[4], out DateTime parsedDate))
                    {
                        eq.NextInspectionDate = parsedDate;
                    }
                }
                else if (type.Equals("standard", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: kode hac, nama equipment, part, standar
                    var hac = cols[0];
                    var partDesc = cols.Length > 2 ? cols[2] : "";
                    var standardText = cols.Length > 3 ? cols[3] : "";

                    var eq = await _context.Equipments
                        .Include(e => e.Tasks)
                        .FirstOrDefaultAsync(e => e.HACCode == hac);
                        
                    if (eq != null && !string.IsNullOrEmpty(partDesc))
                    {
                        var task = eq.Tasks.FirstOrDefault(t => t.Description == partDesc);
                        
                        // If part doesn't exist, create it
                        if (task == null)
                        {
                            task = new InspectionTask { Description = partDesc, EquipmentId = eq.Id };
                            _context.InspectionTasks.Add(task);
                            await _context.SaveChangesAsync();
                            added++;
                        }

                        if (!string.IsNullOrEmpty(standardText))
                        {
                            if (!await _context.TaskStandards.AnyAsync(s => s.InspectionTaskId == task.Id && s.StandardText == standardText))
                            {
                                _context.TaskStandards.Add(new TaskStandard { StandardText = standardText, InspectionTaskId = task.Id });
                                added++;
                            }
                            else { skippedCount++; }
                        }
                    }
                    else { skippedCount++; }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Successfully imported {added} records. (Ignored/Existing/Invalid: {skippedCount})" });
        }
    }
}
