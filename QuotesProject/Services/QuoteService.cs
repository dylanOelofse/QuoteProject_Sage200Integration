using QuotesProject.Data;
using QuotesProject.Interfaces;
using QuotesProject.Models;
using System.Data;

namespace QuotesProject.Services
{
    public class QuoteService : IQuoteService
    {
        public List<Quote> GetQuotes()
        {
            DataTable quotesTable = DatabaseEngine.GetAllQuotes();

            var quotes = new List<Quote>();

            //for (int i = 0; i < 60; i++)
            //{
            //    DataRow row = quotesTable.Rows[i];
            //    quotes.Add(new Quote
            //    {
            //        Id = Convert.ToInt32(row["QuoteId"]),
            //        QuoteNumber = row["QuoteNumber"].ToString(),
            //        Customer = row["Customer"].ToString(),
            //        Address = row["Address"].ToString(),
            //        ExternalOrderNumber = row["ExternalOrderNumber"].ToString(),
            //        OrderDate = row["OrderDate"] == DBNull.Value ? null : Convert.ToDateTime(row["OrderDate"]),
            //        DueDate = row["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(row["DueDate"]),
            //        InvoiceDate = row["InvoiceDate"] == DBNull.Value ? null : Convert.ToDateTime(row["InvoiceDate"]),
            //    });
            //}

            foreach (DataRow row in quotesTable.Rows)
            {
                quotes.Add(new Quote
                {
                    Id = Convert.ToInt32(row["QuoteId"]),
                    QuoteNumber = row["QuoteNumber"].ToString(),
                    Customer = row["Customer"].ToString(),
                    Address = row["Address"].ToString(),
                    ExternalOrderNumber = row["ExternalOrderNumber"].ToString(),
                    OrderDate = row["OrderDate"] == DBNull.Value ? null : Convert.ToDateTime(row["OrderDate"]),
                    DueDate = row["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(row["DueDate"]),
                    InvoiceDate = row["InvoiceDate"] == DBNull.Value ? null : Convert.ToDateTime(row["InvoiceDate"]),
                });
            }

            return quotes;
        }

        public Quote GetQuoteById(int quoteId)
        {
            DataTable quoteTable = DatabaseEngine.GetQuoteById(quoteId);

            if (quoteTable.Rows.Count == 0)
                throw new KeyNotFoundException("Quote not found"); 

            DataRow row = quoteTable.Rows[0];

            Quote quote = new Quote
            {
                Id = Convert.ToInt32(row["QuoteId"]),
                QuoteNumber = row["QuoteNumber"].ToString(),
                Customer = row["Customer"].ToString(),
                Address = row["Address"].ToString(),
                ExternalOrderNumber = row["ExternalOrderNumber"].ToString(),
                OrderDate = row["OrderDate"] == DBNull.Value ? null : Convert.ToDateTime(row["OrderDate"]),
                DueDate = row["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(row["DueDate"]),
                InvoiceDate = row["InvoiceDate"] == DBNull.Value ? null : Convert.ToDateTime(row["InvoiceDate"]),
            };

            return quote;
        }

        public void CreateQuote(Quote quote)
        {
            if (quote == null)
                throw new ArgumentNullException(nameof(quote));

            if (string.IsNullOrWhiteSpace(quote.Customer))
                throw new ArgumentException("Customer name is required.");

            DatabaseEngine.CreateQuote(quote); 
        }

        public void UpdateQuote(Quote quote)
        {
            if (quote == null)
                throw new ArgumentNullException(nameof(quote));

            if (quote.Id <= 0)
                throw new ArgumentException("A valid quote Id is required.");

            DatabaseEngine.UpdateQuote(quote);
        }

        public void DeleteQuote(int quoteId)
        {
            if (quoteId <= 0)
                throw new ArgumentException("A valid quote Id is required.");

            DatabaseEngine.DeleteQuote(quoteId);
        }
    }   
}  
