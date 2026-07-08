using IntegrationService;
using Microsoft.Data.SqlClient;
using Pastel.Evolution;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;

namespace IntegrationPollingService
{
    public class SyncSageQuotes
    {
        public static string companyDB, commonDB, server, userName, password, serialNumber, authKey;

        static SyncSageQuotes()
        {
            var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "settings1.json"));
            var cfg = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            companyDB = cfg["CompanyDB"];
            commonDB = cfg["CommonDB"];
            server = cfg["Server"];
            userName = cfg["UserName"];
            password = cfg["Password"];
            serialNumber = cfg["SerialNumber"];
            authKey = cfg["AuthKey"];
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

        public static void crmToStaging()
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public static void stagingToSage()
        {
            try
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
                    sageQuote.DeliverTo = new Address(quoteHeader.Rows[0]["Address"].ToString(), "", "");
                    //sageQuote.DeliverTo = sageQuote.Customer.DefaultDeliveryAddress.Condense();
                    //sageQuote.InvoiceTo = sageQuote.Customer.PostalAddress.Condense();
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

                    //foreach (DataRow line in quoteLines.Rows) // Alternative
                    //{
                    //    sageQuote.Detail.Add(new OrderDetail
                    //    {
                    //        InventoryItem = new InventoryItem(line["Item"].ToString()),
                    //        Quantity = Convert.ToDouble(line["Quantity"]),
                    //        UnitSellingPrice = Convert.ToDouble(line["Price"]),
                    //        DiscountPercent = Convert.ToDouble(line["Discount"])
                    //    });
                    //}

                    sageQuote.Save();
                    //sageQuote.Process();

                    int sageQuoteId = sageQuote.ID;
                    string sageQuoteNum = sageQuote.OrderNo;

                    DatabaseEngine.updateCreatedQuotes(sageQuoteId, sageQuoteNum, Convert.ToInt32(row["CrmQuoteId"]), "y", "Success");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static void updateQuotes()
        {
            DatabaseContext.Initialise(companyDB, commonDB, server, userName, password, serialNumber, authKey);

            DataTable updatePendingQuotes = new DataTable();

            updatePendingQuotes = DatabaseEngine.getUpdatePendingQuotes();

            foreach (DataRow row in updatePendingQuotes.Rows)
            {
                int sageQuoteId = Convert.ToInt32(row["SageQuoteId"]);
                int QuoteId = Convert.ToInt32(row["QuoteId"]);

                DataTable dtQuoteHeader = DatabaseEngine.GetQuoteById(QuoteId);
                DataTable dtQuoteLines = DatabaseEngine.GetQuoteLines(QuoteId);

                SalesOrder sageQuote = new SalesOrder(sageQuoteId);

                sageQuote.Customer = new Customer(dtQuoteHeader.Rows[0]["Customer"].ToString());
                sageQuote.ExternalOrderNo = dtQuoteHeader.Rows[0]["ExternalOrderNumber"].ToString();
                sageQuote.OrderDate = Convert.ToDateTime(dtQuoteHeader.Rows[0]["OrderDate"]);
                sageQuote.DueDate = Convert.ToDateTime(dtQuoteHeader.Rows[0]["DueDate"]);
                sageQuote.InvoiceDate = Convert.ToDateTime(dtQuoteHeader.Rows[0]["InvoiceDate"]);

                sageQuote.Detail.Clear(); //Clear existing lines and repopulate with updated lines

                foreach (DataRow line in dtQuoteLines.Rows)
                {
                    sageQuote.Detail.Add(new OrderDetail
                    {
                        InventoryItem = new InventoryItem(line["Item"].ToString()),
                        Quantity = Convert.ToDouble(line["Quantity"]),
                        UnitSellingPrice = Convert.ToDouble(line["Price"]),
                        DiscountPercent = Convert.ToDouble(line["Discount"])
                    });
                }
                
                sageQuote.Save();
                //sageQuote.Process();

                DatabaseEngine.updateQuoteFlag("us", QuoteId);
            }
        }

        public static void deleteQuote()
        {
            DatabaseContext.Initialise(companyDB, commonDB, server, userName, password, serialNumber, authKey);

            DataTable deletePendingQuotes = new DataTable();

            deletePendingQuotes = DatabaseEngine.getDeletePendingQuotes();

            foreach (DataRow row in deletePendingQuotes.Rows)
            {
                int sageQuoteId = Convert.ToInt32(row["SageQuoteId"]);
                int QuoteId = Convert.ToInt32(row["QuoteId"]);

                SalesOrderQuotation sageQuote = new SalesOrderQuotation(sageQuoteId);
                sageQuote.Delete();

                DatabaseEngine.updateQuoteFlag("ds", QuoteId);
            }
        }
    }
}
