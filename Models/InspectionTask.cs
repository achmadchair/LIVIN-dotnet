namespace Livin.Api.Models
{
    public class InspectionTask
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        
        public int PartId { get; set; }
        public Part? Part { get; set; }
        
        public string Type { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        
        public string HACCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        
        public ICollection<TaskStandard> Standards { get; set; } = new List<TaskStandard>();
    }
}
