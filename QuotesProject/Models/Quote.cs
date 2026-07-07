namespace QuotesProject.Models
{
    public class Quote
    {
        public int Id { get; set; }
        public string? QuoteNumber { get; set; }
        public string Customer { get; set; }
        public string? Address { get; set; }
        public string? ExternalOrderNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? Flag { get; set; }
    }
}
