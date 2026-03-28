using Livin.Api.Data;
using Livin.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Livin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Leader")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Site)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.InspectorCategory,
                    Role = u.Role.ToString(),
                    u.SiteId,
                    SiteName = u.Site != null ? u.Site.Name : "Global"
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return BadRequest("Username and Password are required.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                return BadRequest("Username already exists.");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User created successfully", UserId = user.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updateData)
        {
            var dbUser = await _context.Users.FindAsync(id);
            if (dbUser == null)
                return NotFound("User not found.");

            // Update allowed fields
            if (!string.IsNullOrWhiteSpace(updateData.Username))
                dbUser.Username = updateData.Username;

            if (!string.IsNullOrWhiteSpace(updateData.PasswordHash))
                dbUser.PasswordHash = updateData.PasswordHash;

            dbUser.FullName = updateData.FullName ?? "";
            dbUser.Email = updateData.Email ?? "";
            dbUser.InspectorCategory = updateData.InspectorCategory ?? "";
            dbUser.Role = updateData.Role;
            
            if (updateData.SiteId > 0)
                dbUser.SiteId = updateData.SiteId;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "User updated successfully" });
        }
    }
}
