using Livin.Api.Models;

namespace Livin.Api.DTOs
{
    public class InspectionSubmissionRequest
    {
        public int EquipmentId { get; set; }
        public InspectionType Type { get; set; }
        public List<InspectionDetailDto> Details { get; set; } = new List<InspectionDetailDto>();
    }

    public class InspectionDetailDto
    {
        public int InspectionTaskId { get; set; }
        public bool IsPassed { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string SelectedTask { get; set; } = string.Empty;
        public int? TaskStandardId { get; set; }
        public string FollowUpAction { get; set; } = string.Empty;
    }
}
