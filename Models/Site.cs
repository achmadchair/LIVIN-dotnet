namespace Livin.Api.Models
{
    public class Site
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}
