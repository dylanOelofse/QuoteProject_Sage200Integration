using QuotesProject.Models;

namespace QuotesProject.Interfaces
{
    public interface IQuoteService
    {
        List<Quote> GetQuotes();

        Quote GetQuoteById(int quoteId);

        void CreateQuote(Quote quote);

        void UpdateQuote(Quote quote);

        void DeleteQuote(int quoteId);
    }
}
