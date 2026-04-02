namespace Livin.Api.Models
{
    public class TaskStandard
    {
        public int Id { get; set; }
        public string StandardText { get; set; } = string.Empty;
        
        public int InspectionTaskId { get; set; }
        public InspectionTask? InspectionTask { get; set; }
        
        public string Type { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        
        public string HACCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
    }
}
