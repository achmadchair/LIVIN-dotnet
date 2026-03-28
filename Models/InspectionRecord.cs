using System;

namespace Livin.Api.Models
{
    public enum InspectionType
    {
        Equipment,
        Safety
    }

    public class InspectionRecord
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }
        
        public int InspectorId { get; set; }
        public User? Inspector { get; set; }
        
        public int SiteId { get; set; }
        public Site? Site { get; set; }
        
        public DateTime InspectionDate { get; set; } = DateTime.UtcNow;
        public InspectionType Type { get; set; }
        
        public ICollection<InspectionDetail> Details { get; set; } = new List<InspectionDetail>();
    }
}
