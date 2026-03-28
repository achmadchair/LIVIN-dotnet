using System;

namespace Livin.Api.Models
{
    public class MaintenanceRecord
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }
        
        public int? InspectionRecordId { get; set; }
        public InspectionRecord? InspectionRecord { get; set; }
        
        public string Remarks { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // Open, In Progress, Completed
        
        public string Technician { get; set; } = string.Empty;
        public string CompletionNotes { get; set; } = string.Empty;
        
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedDate { get; set; }
    }
}
