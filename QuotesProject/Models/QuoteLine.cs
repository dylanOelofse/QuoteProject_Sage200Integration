namespace QuotesProject.Models
{
    public class QuoteLine
    {
        public int? LineId { get; set; }
        public int QuoteId { get; set; }
        public string Item { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
    }
}
