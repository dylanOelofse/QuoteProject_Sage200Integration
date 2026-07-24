using Microsoft.Data.SqlClient;
using QuotesProject.Models;
using System.Data;

namespace QuotesProject.Data
{
    public class DatabaseEngine
    {
        private static string _connectionString;

        public static void Initialize(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private static SqlConnection InitialiseConnection()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Connection string has not been initialized.");

            return new SqlConnection(_connectionString);
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

        public static void UpdatePasswordHash(int userId, string newPasswordHash)
        {
            string query = @"
                            UPDATE      Users
                            SET         Password = @Password
                            WHERE       UserId = @UserId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Password", newPasswordHash);

            ExecuteNonQuery(command);
        }

        //Users

        public static DataTable GetUserByUsername(string username)
        {
            string query = @"
                            SELECT
                                UserId,
                                UserName,
                                Password,
                                IsAdmin
                            FROM Users
                            WHERE UserName = @UserName";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@UserName", username);

            return ExecuteDataTable(command);
        }

        public static void CreateUser(User user)
        {
            string query = @"
                        INSERT INTO Users
                        (
                        Username,
                        Password,
                        IsAdmin
                        )
                        VALUES
                        (
                        @Username,
                        @Password,
                        @IsAdmin
                        )";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.Parameters.AddWithValue("@IsAdmin", user.IsAdmin);

            ExecuteNonQuery(command);
        }


        //Quotes

        public static DataTable GetAllQuotes()
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
                            FROM Quote
                            WHERE Flag IS NULL OR Flag = 'us' OR Flag = 'up'
                            ORDER BY QuoteId DESC";

            using var command = new SqlCommand(query);

            return ExecuteDataTable(command);
        }

        public static DataTable GetQuoteById(int quoteId)
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
                            FROM Quote
                            WHERE QuoteId = @QuoteId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@QuoteId", quoteId);

            return ExecuteDataTable(command);
        }

        public static void CreateQuote(Quote quote)
        {
            string query = @"
                            INSERT INTO Quote
                            (
                                Customer,
                                Address,
                                ExternalOrderNumber,
                                OrderDate,
                                DueDate,
                                InvoiceDate
                            )
                            VALUES
                            (
                                @Customer,
                                @Address,
                                @ExternalOrderNumber,
                                @OrderDate,
                                @DueDate,
                                @InvoiceDate
                            )";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@Customer", quote.Customer);
            command.Parameters.AddWithValue("@Address", quote.Address);
            command.Parameters.AddWithValue("@ExternalOrderNumber", quote.ExternalOrderNumber);

            command.Parameters.AddWithValue("@OrderDate", (object?)quote.OrderDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@DueDate", (object?)quote.DueDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@InvoiceDate", (object?)quote.InvoiceDate ?? DBNull.Value);

            ExecuteNonQuery(command);
        }

        public static void UpdateQuote(Quote quote)
        {
            string query = @"
                            UPDATE Quote
                            SET
                                Customer = @Customer,
                                Address = @Address,
                                ExternalOrderNumber = @ExternalOrderNumber,
                                OrderDate = @OrderDate,
                                DueDate = @DueDate,
                                InvoiceDate = @InvoiceDate,
                                Flag = @Flag
                            WHERE
                                QuoteId = @QuoteId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@QuoteId", quote.Id);
            command.Parameters.AddWithValue("@Customer", quote.Customer);
            command.Parameters.AddWithValue("@Address", quote.Address);
            command.Parameters.AddWithValue("@ExternalOrderNumber", quote.ExternalOrderNumber);

            command.Parameters.AddWithValue("@OrderDate", (object?)quote.OrderDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@DueDate", (object?)quote.DueDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@InvoiceDate", (object?)quote.InvoiceDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Flag", "up");

            ExecuteNonQuery(command);
        }

        public static void DeleteQuote(int quoteId)
        {
            string query = @"
                            UPDATE		Quote
                            SET			Flag = 'dp'
                            WHERE		QuoteId = @QuoteId;

                            UPDATE      QuoteLine
                            SET         Flag = 'dp'
                            WHERE       QuoteId = @QuoteId;";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@QuoteId", quoteId);

            ExecuteNonQuery(command);
        }


        //Quote Lines

        public static DataTable GetQuoteLines(int quoteId)
        {
            string query = @"
                            SELECT
                                LineId,
                                Item,
                                Quantity,
                                Price,
                                Discount
                            FROM QuoteLine
                            WHERE QuoteId = @QuoteId
                            AND (Flag IS NULL OR Flag = 'us')
                            ORDER BY LineId DESC";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@QuoteId", quoteId);

            return ExecuteDataTable(command);
        }

        public static DataTable GetQuoteLineById(int lineId)
        {
            string query = @"
                            SELECT
                                QuoteId,
                                Item,
                                Quantity,
                                Price,
                                Discount
                            FROM QuoteLine
                            WHERE lineId = @lineId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@lineId", lineId);

            return ExecuteDataTable(command);
        }

        public static void CreateQuoteLine(QuoteLine quoteLine)
        {
            string query = @"
                            INSERT INTO QuoteLine
                            (
                                QuoteId,
                                Item,
                                Quantity,
                                Price,
                                Discount
                            )
                            VALUES
                            (
                                @QuoteId,
                                @Item,
                                @Quantity,
                                @Price,
                                @Discount
                            )
    
                            UPDATE Quote 
                            SET Flag = 'up'
                            WHERE QuoteId = @QuoteId";
                            

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@QuoteId", quoteLine.QuoteId);
            command.Parameters.AddWithValue("@Item", quoteLine.Item);
            command.Parameters.AddWithValue("@Quantity", quoteLine.Quantity);
            command.Parameters.AddWithValue("@Price", quoteLine.Price);
            command.Parameters.AddWithValue("@Discount", quoteLine.Discount);

            ExecuteNonQuery(command);
        }

        public static void UpdateQuoteLine(QuoteLine quoteLine)
        {
            string query = @"
                            UPDATE QuoteLine
                            SET
                                Item = @Item,
                                Quantity = @Quantity,
                                Price = @Price,
                                Discount = @Discount
                            WHERE
                                LineId = @LineId

                            UPDATE Quote
                            SET
                                Flag = 'up'
                            WHERE 
                                QuoteId = @QuoteId";

            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@LineId", quoteLine.LineId);
            command.Parameters.AddWithValue("@Item", quoteLine.Item);
            command.Parameters.AddWithValue("@Quantity", quoteLine.Quantity);
            command.Parameters.AddWithValue("@Price", quoteLine.Price);
            command.Parameters.AddWithValue("@Discount", quoteLine.Discount);

            command.Parameters.AddWithValue("@QuoteId", quoteLine.QuoteId);

            ExecuteNonQuery(command);
        }

        public static void DeleteQuoteLine(int lineId)
        {
            string query = @"
                            UPDATE      QuoteLine
                            SET         Flag = 'dp'
                            WHERE       lineId = @lineId

                            UPDATE      Quote
                            SET         Flag = 'up'
                            WHERE       QuoteId =  (SELECT QuoteId 
                                                    FROM QuoteLine
                                                    WHERE lineId = @lineId)";


            using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@lineId", lineId);

            ExecuteNonQuery(command);
        }

    }
}