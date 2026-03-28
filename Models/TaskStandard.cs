namespace Livin.Api.Models
{
    public class TaskStandard
    {
        public int Id { get; set; }
        public string StandardText { get; set; } = string.Empty;
        
        public int InspectionTaskId { get; set; }
        public InspectionTask? InspectionTask { get; set; }
    }
}
