using IntegrationPollingService;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;
using Microsoft.Win32;
using System.Data;

namespace IntegrationService
{
    public class DatabaseEngine
    {
        static SqlConnection DatabaseConnection = new SqlConnection();
        static String crmDB = "QuoteProjectOriginal";
        static string sageDB = "NECSA Laboratories_Live";

        public static SqlConnection InitialiseConnection()
        {

            string user = "sa";
            string database = "QuoteProjectOriginal";
            string server = "DYLANOELOFSE";
            string Password = "1234";

            string ConnectionString = $@"data source={server};initial catalog = {database};Integrated Security =false ;user={user};password={Password};MultipleActiveResultSets=True;TrustServerCertificate=True;";
            DatabaseConnection = new SqlConnection(ConnectionString);

            return DatabaseConnection;
        }

        public static void ExecuteNonQuery(SqlCommand command)
        {
            try
            {
                using (var connection = InitialiseConnection())
                {
                    command.Connection = connection;

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ExecuteNonQuery failed: " + ex.Message, ex);
            }
        }

        public static object? ExecuteScalar(SqlCommand command)
        {
            try
            {
                using (var connection = InitialiseConnection())
                {
                    command.Connection = connection;

                    connection.Open();
                    return command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ExecuteScalar failed: " + ex.Message, ex);
            }
        }

        public static DataTable ExecuteDataTable(SqlCommand command)
        {
            try
            {
                using (var connection = InitialiseConnection())
                {
                    command.Connection = connection;

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        return table;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ExecuteDataTable failed: " + ex.Message, ex);
            }
        }

        //Quotes

        public static DataTable SyncAllQuotes()
        {
            string query = $@"
                            SELECT		    i.AutoIndex, c.Account, i.Address1, i.ExtOrderNum, i.OrderDate, i.DueDate, i.InvDate, i.OrderNum
                            FROM            [{sageDB}].dbo.InvNum i
                            INNER JOIN      [{sageDB}].dbo.Client c ON i.AccountID = c.DCLink
                            LEFT JOIN       [{crmDB}].dbo.Quote q 
                                            ON q.QuoteNumber COLLATE DATABASE_DEFAULT = i.OrderNum COLLATE DATABASE_DEFAULT
                            WHERE           i.DocType = 4
                            AND             i.DocState = 1
                            AND             q.QuoteNumber IS NULL";

            using var command = new SqlCommand(query);

            return ExecuteDataTable(command);
        }

        public static DataTable SyncAllQuoteLineByQuoteId(int sageQuoteId)
        {
            string query = $@"
                            SELECT			S.Code, il.fQuantity, il.fUnitPriceIncl, il.fLineDiscount
                            FROM			[{sageDB}].dbo._btblInvoiceLines il
                            INNER JOIN      [{sageDB}].dbo.StkItem s ON  s.StockLink = il.iStockCodeID
                            INNER JOIN      [{sageDB}].dbo.InvNum i on i.AutoIndex = il.iInvoiceID
                            LEFT JOIN       [{crmDB}].dbo.QuoteStaging2 qs ON i.AutoIndex = qs.SageQuoteId
                            WHERE           il.iInvoiceID = @SageQuoteId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@SageQuoteId", sageQuoteId);

            return ExecuteDataTable(command);
        }

        public static int InsertSageQuoteToCrm(string customer, string address, string externalOrderNumber, DateTime orderDate, DateTime dueDate, DateTime invoiceDate, string orderNum)
        {
            string query = $@"
                    INSERT INTO         [{crmDB}].dbo.Quote 
                                        (Customer, Address, ExternalOrderNumber, OrderDate, DueDate, InvoiceDate, QuoteNumber)
                    VALUES              (@Customer, @Address, @ExternalOrderNumber, @OrderDate, @DueDate, @InvoiceDate, @QuoteNumber);
                    SELECT CAST         (SCOPE_IDENTITY() AS int);";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@Customer", customer);
            command.Parameters.AddWithValue("@Address", address);
            command.Parameters.AddWithValue("@ExternalOrderNumber", externalOrderNumber);
            command.Parameters.AddWithValue("@OrderDate", orderDate);
            command.Parameters.AddWithValue("@DueDate", dueDate);
            command.Parameters.AddWithValue("@InvoiceDate", invoiceDate);
            command.Parameters.AddWithValue("@QuoteNumber", orderNum);

            return Convert.ToInt32(ExecuteScalar(command));
        }

        public static void InsertSageQuoteLineToCrm(int quoteId, string item, decimal quantity, decimal price, decimal discount)
        {
            string query = $@"
                            INSERT INTO         [{crmDB}].dbo.QuoteLine 
                                                (QuoteId, Item, Quantity, Price, Discount)
                            VALUES				(@QuoteId, @Item, @Quantity, @Price, @Discount)";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@QuoteId", quoteId);
            command.Parameters.AddWithValue("@Item", item);
            command.Parameters.AddWithValue("@Quantity", quantity);
            command.Parameters.AddWithValue("@Price", price);
            command.Parameters.AddWithValue("@Discount", discount);

            ExecuteNonQuery(command);
        }

        public static void InsertNewQuotesFromSage(int sageQuoteId, int CrmQuoteId, string flag, string message)
        {
            string query = $@"
                            INSERT INTO         [{crmDB}].dbo.QuoteStaging2 
					                            (SageQuoteId, CrmQuoteId, Flag, [Message])
                            VALUES				(@SageQuoteId, @CrmQuoteId, @Flag, @Message)  ";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@SageQuoteId", sageQuoteId);
            command.Parameters.AddWithValue("@CrmQuoteId", CrmQuoteId);
            command.Parameters.AddWithValue("@Flag", flag);
            command.Parameters.AddWithValue("@Message", message);

            ExecuteNonQuery(command);
        }

        public static DataTable GetNewQuotes()
        {
            string query = $@"
                            SELECT DISTINCT		q.QuoteId, qs.CrmQuoteId, qs.Flag
                            FROM				Quote q
                            LEFT JOIN			QuoteStaging2 qs on q.QuoteId = qs.CrmQuoteId
                            INNER JOIN			QuoteLine ql on ql.QuoteId = q.QuoteId
                            						AND ISNULL(ql.Flag, '') <> 'dp'
                            WHERE				qs.CrmQuoteId is null
                            AND					ISNULL(q.Flag, '') <> 'dp'";

            using var command = new SqlCommand(query);

            return ExecuteDataTable(command);
        }

        public static void InsertNewQuotes(int CrmQuoteId, string flag, string message)
        {
            string query = $@"
                            INSERT INTO         QuoteStaging2 
					                            (CrmQuoteId, Flag, [Message])
                            VALUES				(@CrmQuoteId, @Flag, @Message)  ";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@CrmQuoteId", CrmQuoteId);
            command.Parameters.AddWithValue("@Flag", flag);
            command.Parameters.AddWithValue("@Message", message );

            ExecuteNonQuery(command);
        }

        public static DataTable GetStagingQuotes()
        {
            string query = $@"
                            SELECT				crmQuoteId
                            FROM				QuoteStaging2
                            WHERE				Flag = 'p'";

            using var command = new SqlCommand(query);

            return ExecuteDataTable(command);
        }

        public static DataTable GetQuoteById(int crmQuoteId)
        {
            string query = @"
                            SELECT
                                    QuoteId,
                                    QuoteNumber,
                                    Customer,
                                    Address,
                                    ExternalOrderNumber,
                                    OrderDate,
                                    DueDate,
                                    InvoiceDate
                            FROM    Quote
                            WHERE   QuoteId = @crmQuoteId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@crmQuoteId", crmQuoteId);

            return ExecuteDataTable(command);
        }

        public static DataTable GetQuoteLines(int crmQuoteId)
        {
            string query = @"
                            SELECT
                                        LineId,
                                        Item,
                                        Quantity,
                                        Price,
                                        Discount,
                                        Flag
                            FROM        QuoteLine
                            WHERE       QuoteId = @crmQuoteId
                            AND         ISNULL(Flag, '') <> 'dp'
                            ORDER BY    LineId DESC";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@crmQuoteId", crmQuoteId);

            return ExecuteDataTable(command);
        }

        public static void updateCreatedQuotes(int sageQuoteId, string sageQuoteNum, int crmQuoteId, string flag, string message)  //change crmQuoteId to sageQuoteId
        {
            string query = $@"
                            UPDATE              Quote
                            SET                 QuoteNumber = @SageQuoteNum
                            WHERE               QuoteId = @CrmQuoteId

                            UPDATE              QuoteStaging2
                            SET                 sageQuoteId = @sageQuoteId, Flag = @Flag, [Message] = @Message
                            WHERE               CrmQuoteId = @CrmQuoteId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@sageQuoteId", sageQuoteId);
            command.Parameters.AddWithValue("@SageQuoteNum", sageQuoteNum);
            command.Parameters.AddWithValue("@CrmQuoteId", crmQuoteId);
            command.Parameters.AddWithValue("@Flag", flag);
            command.Parameters.AddWithValue("@Message", message);

            ExecuteNonQuery(command);
        }

        public static DataTable getUpdatePendingQuotes()
        {
            string query = $@"
                            SELECT          q.QuoteId, qs.sageQuoteId
                            FROM			QuoteStaging2 qs
                            INNER JOIN		Quote q ON qs.CrmQuoteId = q.QuoteId
                            WHERE           q.Flag = 'up'";

            using var command = new SqlCommand(query);

            return ExecuteDataTable(command);
        }

        public static DataTable getDeletePendingQuotes()
        {
            string query = $@"
                            SELECT          q.QuoteId, qs.sageQuoteId
                            FROM			QuoteStaging2 qs
                            INNER JOIN		Quote q ON qs.CrmQuoteId = q.QuoteId
                            WHERE           q.Flag = 'dp'";

            using var command = new SqlCommand(query);

            return ExecuteDataTable(command);
        }

        // Quotes that were pushed to / pulled from Sage but no longer exist there as an open
        // quote - either deleted, or converted to an order/invoice (DocType/DocState changed).
        // These are flagged so the CRM list hides them; nothing is pushed back to Sage.
        public static DataTable getConvertedOrDeletedQuotes()
        {
            string query = $@"
                            SELECT DISTINCT		q.QuoteId
                            FROM				[{crmDB}].dbo.Quote q
                            INNER JOIN			[{crmDB}].dbo.QuoteStaging2 qs on qs.CrmQuoteId = q.QuoteId
                            LEFT JOIN			[{sageDB}].dbo.InvNum i
                            						ON i.OrderNum COLLATE DATABASE_DEFAULT = q.QuoteNumber COLLATE DATABASE_DEFAULT
                            						AND i.DocType = 4
                            						AND i.DocState = 1
                            WHERE				i.AutoIndex is null
                            AND					NULLIF(LTRIM(RTRIM(q.QuoteNumber)), '') is not null
                            AND					ISNULL(q.Flag, '') not in ('dp', 'ds', 'cv')
                            AND					ISNULL(qs.Flag, '') <> 'p'";

            using var command = new SqlCommand(query);

            return ExecuteDataTable(command);
        }

        public static void updateQuoteFlag(string flag, int quoteId)
        {
            string query = $@"
                            UPDATE			Quote
                            SET				Flag = @Flag
                            WHERE			QuoteId = @QuoteId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@QuoteId", quoteId);
            command.Parameters.AddWithValue("@Flag", flag);

            ExecuteNonQuery(command);
        }
    }
}