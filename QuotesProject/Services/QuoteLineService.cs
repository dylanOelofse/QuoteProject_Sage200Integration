using QuotesProject.Data;
using QuotesProject.Interfaces;
using QuotesProject.Models;
using System.Data;

namespace QuotesProject.Services
{
    public class QuoteLineService : IQuoteLineService
    {
        public List<QuoteLine> GetQuoteLines(int quoteId)
        {
            DataTable quoteLinesTable = DatabaseEngine.GetQuoteLines(quoteId);

            var quoteLines = new List<QuoteLine>();

            foreach (DataRow row in quoteLinesTable.Rows)
            {
                quoteLines.Add(new QuoteLine
                {
                    LineId = Convert.ToInt32(row["LineId"]),
                    QuoteId = quoteId,
                    Item = row["Item"].ToString(),
                    Quantity = Convert.ToDecimal(row["Quantity"]),
                    Price = Convert.ToDecimal(row["Price"]),
                    Discount = Convert.ToDecimal(row["Discount"])
                });
            }

            //var quoteLines = quoteLinesTable.AsEnumerable().Select(row => new QuoteLine
            //{
            //    LineId = Convert.ToInt32(row["LineId"]),
            //    QuoteId = quoteId,
            //    Item = row["Item"].ToString(),
            //    Quantity = Convert.ToDecimal(row["Quantity"]),
            //    Price = Convert.ToDecimal(row["Price"]),
            //    Discount = Convert.ToDecimal(row["Discount"])
            //})
            //.ToList();

            return quoteLines;
        }

        public QuoteLine GetQuoteLineById(int lineId)
        {
            DataTable quoteLineTable = DatabaseEngine.GetQuoteLineById(lineId);

            if (quoteLineTable.Rows.Count == 0)
                throw new KeyNotFoundException("Quote line not found.");

            DataRow row = quoteLineTable.Rows[0];

            return new QuoteLine
            {
                LineId = lineId,
                QuoteId = Convert.ToInt32(row["QuoteId"]),
                Item = row["Item"].ToString(),
                Quantity = Convert.ToDecimal(row["Quantity"]),
                Price = Convert.ToDecimal(row["Price"]),
                Discount = Convert.ToDecimal(row["Discount"])
            };
        }

        public void CreateQuoteLine(QuoteLine quoteLine)
        {
            if (quoteLine == null)
                throw new ArgumentNullException(nameof(quoteLine));

            if (quoteLine.QuoteId <= 0)
                throw new ArgumentException("A valid quote is required.");

            if (string.IsNullOrWhiteSpace(quoteLine.Item))
                throw new ArgumentException("Item is required.");

            DatabaseEngine.CreateQuoteLine(quoteLine);
        }

        public void UpdateQuoteLine(QuoteLine quoteLine)
        {
            if (quoteLine == null)
                throw new ArgumentNullException(nameof(quoteLine));

            if (string.IsNullOrWhiteSpace(quoteLine.Item))
                throw new ArgumentException("Item is required.");

            DatabaseEngine.UpdateQuoteLine(quoteLine);
        }

        public void DeleteQuoteLine(int lineId)
        {
            if (lineId <= 0)
                throw new ArgumentException("A valid line Id is required.");

            DatabaseEngine.DeleteQuoteLine(lineId);
        }
    }
}
