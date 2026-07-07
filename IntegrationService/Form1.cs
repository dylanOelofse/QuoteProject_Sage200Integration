using Pastel.Evolution;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.Json;

namespace IntegrationService
{
    public partial class Form1 : Form
    {
        public static string companyDB, commonDB, server, userName, password, serialNumber, authKey;

        public Form1()
        {
            InitializeComponent();
        
            var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "settings.json"));
            var cfg = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            companyDB = cfg["CompanyDB"];
            commonDB = cfg["CommonDB"];
            server = cfg["Server"];
            userName = cfg["UserName"];
            password = cfg["Password"];
            serialNumber = cfg["SerialNumber"];
            authKey = cfg["AuthKey"];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            crmtostaging();
        }

        public void crmtostaging()
        {
            DataTable newQuotes = new DataTable();

            newQuotes = DatabaseEngine.GetNewQuotes();

            foreach (DataRow row in newQuotes.Rows)
            {
                int crmQuoteId = Convert.ToInt32(row["QuoteId"]);
                string flag = "p";
                string message = "Pending";

                DatabaseEngine.InsertNewQuotes(crmQuoteId, flag, message);
            }
        }

        public void stagingToSage()
        {
            DatabaseContext.Initialise(companyDB, commonDB, server, userName, password, serialNumber, authKey);

            DataTable newQuotes = new DataTable();

            newQuotes = DatabaseEngine.GetStagingQuotes();

            foreach (DataRow row in newQuotes.Rows)
            {
                DataTable quoteHeader = DatabaseEngine.GetQuoteById(Convert.ToInt32(row["CrmQuoteId"]));
                DataTable quoteLines = DatabaseEngine.GetQuoteLines(Convert.ToInt32(row["CrmQuoteId"]));

                SalesOrder sageQuote = new SalesOrder();
                sageQuote.Customer = new Customer(quoteHeader.Rows[0]["Customer"].ToString());
                sageQuote.ExternalOrderNo = quoteHeader.Rows[0]["ExternalOrderNumber"].ToString();
                sageQuote.OrderDate = Convert.ToDateTime(quoteHeader.Rows[0]["OrderDate"]);
                sageQuote.DueDate = Convert.ToDateTime(quoteHeader.Rows[0]["DueDate"]);
                sageQuote.InvoiceDate = Convert.ToDateTime(quoteHeader.Rows[0]["InvoiceDate"]);

                foreach (var line in quoteLines.AsEnumerable())
                {
                    sageQuote.Detail.Add(new OrderDetail
                    {
                        InventoryItem = new InventoryItem(line.Field<string>("Item")),
                        Quantity = Convert.ToDouble(line.Field<decimal>("Quantity")),
                        UnitSellingPrice = Convert.ToDouble(line.Field<decimal>("Price")),
                        DiscountPercent = Convert.ToDouble(line.Field<decimal>("Discount"))
                    });
                }

                sageQuote.Save();

                int sageQuoteId = sageQuote.ID;
                string sageQuoteNum = sageQuote.OrderNo;

                DatabaseEngine.updateCreatedQuotes(sageQuoteId, sageQuoteNum, Convert.ToInt32(row["CrmQuoteId"]), "y", "Success");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stagingToSage();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SageToCrm();
        }

        public static void SageToCrm()
        {
            try
            {
                DatabaseContext.Initialise(companyDB, commonDB, server, userName, password, serialNumber, authKey);

                DataTable existingSageQuotes = new DataTable();
                existingSageQuotes = DatabaseEngine.SyncAllQuotes();

                foreach (DataRow row in existingSageQuotes.Rows)
                {
                    int sageQuoteId = Convert.ToInt32(row["AutoIndex"]);
                    string orderNum = row["OrderNum"].ToString();
                    string customer = row["Account"].ToString();
                    string address = row["Address1"].ToString();
                    string externalOrderNumber = row["ExtOrderNum"].ToString();
                    DateTime orderDate = Convert.ToDateTime(row["OrderDate"]);
                    DateTime dueDate = Convert.ToDateTime(row["DueDate"]);
                    DateTime invDate = Convert.ToDateTime(row["InvDate"]);

                    DataTable exisitngQuotesLines = new DataTable();
                    exisitngQuotesLines = DatabaseEngine.SyncAllQuoteLineByQuoteId(sageQuoteId);

                    int crmQuoteId = DatabaseEngine.InsertSageQuoteToCrm(customer, address, externalOrderNumber, orderDate, dueDate, invDate, orderNum);
                    DatabaseEngine.InsertNewQuotesFromSage(sageQuoteId, crmQuoteId, "y", "Success");

                    foreach (DataRow line in exisitngQuotesLines.Rows)
                    {
                        string item = line["Code"].ToString();
                        decimal quantity = Convert.ToDecimal(line["fQuantity"]);
                        decimal price = Convert.ToDecimal(line["fUnitPriceIncl"]);
                        decimal discount = Convert.ToDecimal(line["fLineDiscount"]);

                        DatabaseEngine.InsertSageQuoteLineToCrm(crmQuoteId, item, quantity, price, discount);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
