namespace QuotesProject.Models
{
    public class QuoteReportModel
    {
        public string? QuoteNumber { get; set; }
        public string Customer { get; set; } = "";
        public string? Address { get; set; }
        public string? ExternalOrderNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? DueDate { get; set; }
        public List<QuoteLine> Lines { get; set; } = new();
    }
}
