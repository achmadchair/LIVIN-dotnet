namespace Livin.Api.Models
{
    public class InspectionDetail
    {
        public int Id { get; set; }
        public int InspectionRecordId { get; set; }
        public InspectionRecord? InspectionRecord { get; set; }
        
        public int InspectionTaskId { get; set; }
        public InspectionTask? InspectionTask { get; set; }
        
        public bool IsPassed { get; set; }
        public string Remarks { get; set; } = string.Empty;
        
        public string SelectedTask { get; set; } = string.Empty;
        
        public int? TaskStandardId { get; set; }
        public TaskStandard? TaskStandard { get; set; }
        
        public string FollowUpAction { get; set; } = string.Empty;
    }
}
