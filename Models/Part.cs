namespace Livin.Api.Models
{
    public class Part
    {
        public int Id { get; set; }
        public string HACCode { get; set; } = string.Empty;
        
        public int SiteId { get; set; }
        public Site? Site { get; set; }
        
        public string Name { get; set; } = string.Empty; // Nama Part
        
        public string Type { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        
        // FK to Equipment
        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }
        
        // Relation to Tasks
        public ICollection<InspectionTask> Tasks { get; set; } = new List<InspectionTask>();
    }
}
