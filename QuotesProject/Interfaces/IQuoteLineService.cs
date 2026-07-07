using QuotesProject.Models;

namespace QuotesProject.Interfaces
{
    public interface IQuoteLineService
    {
        List<QuoteLine> GetQuoteLines(int quoteId);

        QuoteLine GetQuoteLineById(int lineId);

        void CreateQuoteLine(QuoteLine quoteLine);

        void UpdateQuoteLine(QuoteLine quoteLine);

        void DeleteQuoteLine(int lineId);
    }
}
