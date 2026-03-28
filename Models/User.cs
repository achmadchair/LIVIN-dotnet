using System.Text.Json.Serialization;

namespace Livin.Api.Models
{
    public enum UserRole
    {
        Leader,
        Inspector
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        
        public string PasswordHash { get; set; } = string.Empty;
        
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // "Equipment" or "Safety"
        public string InspectorCategory { get; set; } = string.Empty;
        
        public UserRole Role { get; set; }
        
        public int SiteId { get; set; }
        public Site? Site { get; set; }
    }
}
