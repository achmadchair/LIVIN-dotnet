namespace Livin.Api.Models
{
    public class InspectionTask
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        
        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }
        
        public ICollection<TaskStandard> Standards { get; set; } = new List<TaskStandard>();
    }
}
