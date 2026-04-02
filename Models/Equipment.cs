namespace Livin.Api.Models
{
    public class Equipment
    {
        public int Id { get; set; }
        public string HACCode { get; set; } = string.Empty; // Unique barcode
        public string Name { get; set; } = string.Empty;
        
        public int SiteId { get; set; }
        public Site? Site { get; set; }
        
        public string Type { get; set; } = string.Empty; // "Inspection" or "Safety"
        public string Group { get; set; } = string.Empty;
        
        public DateTime? NextInspectionDate { get; set; }
        
        public ICollection<Part> Parts { get; set; } = new List<Part>();
    }
}
