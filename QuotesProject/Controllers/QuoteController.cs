using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuotesProject.Interfaces;
using QuotesProject.Models;
using QuotesProject.Reports;
using System.Diagnostics;
using System.IO.Compression;
using ClosedXML.Excel;
using DevExpress.DataAccess.ObjectBinding;

namespace QuotesProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class QuoteController : Controller
    {
        private readonly IQuoteService _quoteService;
        private readonly IQuoteLineService _quoteLineService;

        public QuoteController(IQuoteService quoteService, IQuoteLineService quoteLineService)
        {
            _quoteService = quoteService;
            _quoteLineService = quoteLineService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var quotes = _quoteService.GetQuotes();
            var viewModel = new QuoteViewModel { Quotes = quotes };
            return View(viewModel);
        }

        [HttpGet("{quoteId}")]
        public IActionResult GetQuoteById(int quoteId)
        {
            try
            {
                var quote = _quoteService.GetQuoteById(quoteId);

                var viewModel = new QuoteViewModel { 
                    Quotes = new List<Quote> { quote } 
                };

                return View(viewModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("Open/{quoteId}")]
        public IActionResult Open(int quoteId)
        {
            if (quoteId <= 0)
                return BadRequest("A valid quote Id is required");

            try
            {
                _quoteService.GetQuoteById(quoteId);            // confirm it exists before storing the id
                HttpContext.Session.SetInt32("OpenQuoteId", quoteId);

                return RedirectToAction("Index", "QuoteLine");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error opening quote: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateQuote(Quote quote)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _quoteService.CreateQuote(quote);
                return Ok(quote);  //since windows.reload on js
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error creating quote: " + ex.Message);
            }
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateQuote(Quote quote)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _quoteService.UpdateQuote(quote);
                return Ok(quote);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating quote: " + ex.Message);
            }
        }

        [HttpDelete("{quoteId}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteQuote(int quoteId)
        {
            if (quoteId <= 0)
                return BadRequest("A valid quote Id is required");

            try
            {
                _quoteService.DeleteQuote(quoteId);
                return Ok("Quote deleted successfully");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error deleting quote: " + ex.Message);
            }
        }

        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("export/{quoteId}")]
        [Authorize]
        public IActionResult ExportQuoteExcel(int quoteId)
        {
            Quote quote;
            List<QuoteLine> quoteLines;

            try
            {
                quote = _quoteService.GetQuoteById(quoteId);
                quoteLines = _quoteLineService.GetQuoteLines(quoteId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote not found");
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Quote");

            ws.Cell("A1").Value = "Quote Details";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            // header fields
            ws.Cell("A3").Value = "Quote Number";
            ws.Cell("B3").Value = quote.QuoteNumber ?? "";

            ws.Cell("A4").Value = "Customer";
            ws.Cell("B4").Value = quote.Customer ?? "";

            ws.Cell("A5").Value = "Address";
            ws.Cell("B5").Value = quote.Address ?? "";

            ws.Cell("A6").Value = "External Order No";
            ws.Cell("B6").Value = quote.ExternalOrderNumber ?? "";

            ws.Cell("A7").Value = "Order Date";
            ws.Cell("B7").Value = quote.OrderDate?.ToString("yyyy-MM-dd") ?? "";

            ws.Cell("A8").Value = "Due Date";
            ws.Cell("B8").Value = quote.DueDate?.ToString("yyyy-MM-dd") ?? "";

            ws.Cell("A9").Value = "Invoice Date";
            ws.Cell("B9").Value = quote.InvoiceDate?.ToString("yyyy-MM-dd") ?? "";

            ws.Range("A3:A9").Style.Font.Bold = true;
            ws.Range("A3:A9").Style.Fill.BackgroundColor = XLColor.LightGray;

            // line fields
            int headerRow = 11;
            ws.Cell(headerRow, 1).Value = "Item";
            ws.Cell(headerRow, 2).Value = "Quantity";
            ws.Cell(headerRow, 3).Value = "Price";
            ws.Cell(headerRow, 4).Value = "Discount";
            ws.Cell(headerRow, 5).Value = "Total";

            var headerRange = ws.Range(headerRow, 1, headerRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int r = headerRow + 1;

            foreach (var line in quoteLines)
            {
                ws.Cell(r, 1).Value = line.Item ?? "";
                ws.Cell(r, 2).Value = line.Quantity;
                ws.Cell(r, 3).Value = line.Price;
                ws.Cell(r, 4).Value = line.Discount;
                ws.Cell(r, 5).Value = line.Quantity * line.Price * (1 - line.Discount);
                r++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"quote {quote.QuoteNumber}.xlsx");
        }

        [HttpGet("exportAll")]
        [Authorize]
        public IActionResult ExportAllQuotesExcel()
        {
            var quotes = _quoteService.GetQuotes();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("All Quotes");

            //headers

            string[] headers = { "Quote Number", "Customer", "Address", "External Order No", "Order Date", "Due Date", "Invoice Date" };

            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(1, c+1).Value = headers[c];
            }

            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int r = 2;

            foreach (var q in quotes)
            {
                ws.Cell(r, 1).Value = q.QuoteNumber ?? "";
                ws.Cell(r, 2).Value = q.Customer ?? "";
                ws.Cell(r, 3).Value = q.Address ?? "";
                ws.Cell(r, 4).Value = q.ExternalOrderNumber ?? "";
                ws.Cell(r, 5).Value = q.OrderDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(r, 6).Value = q.DueDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(r, 7).Value = q.InvoiceDate?.ToString("yyyy-MM-dd") ?? "";
                r++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "All Quotes.xlsx");
        }

        // ---- Statements ----

        private (string fileName, byte[] pdf) BuildStatement(int quoteId)
        {
            Quote quote = _quoteService.GetQuoteById(quoteId);
            List<QuoteLine> lines = _quoteLineService.GetQuoteLines(quoteId);

            var model = new QuoteReportModel
            {
                QuoteNumber = quote.QuoteNumber,
                Customer = quote.Customer,
                Address = quote.Address,
                ExternalOrderNumber = quote.ExternalOrderNumber,
                OrderDate = quote.OrderDate,
                DueDate = quote.DueDate,
                Lines = lines
            };

            using var report = new QuoteReport();

            //Fill ObjectDataSource that  report uses with data
            var dataSource = report.ComponentStorage.OfType<ObjectDataSource>().First();
            dataSource.DataSource = new List<QuoteReportModel> { model };

            report.CreateDocument();

            using var ms = new MemoryStream();
            report.ExportToPdf(ms);

            // quote numbers come from Sage, strip anything a file name can't hold 
            string name = string.IsNullOrWhiteSpace(quote.QuoteNumber) ? $"Quote-{quote.Id}" : quote.QuoteNumber;
            name = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

            return ($"{name}.pdf", ms.ToArray());
        }

        [HttpPost("generateStatement")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult GenerateStatement([FromBody] int[] quoteIds)
        {
            if (quoteIds == null || quoteIds.Length == 0)
                return BadRequest("No quotes selected.");

            try
            {
                if (quoteIds.Length == 1)
                {
                    var (fileName, pdf) = BuildStatement(quoteIds[0]);
                    return File(pdf, "application/pdf", fileName);
                }

                // several quotes = a flat zip: just the PDFs at the root, no folders
                using var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (int id in quoteIds)
                    {
                        var (fileName, pdf) = BuildStatement(id);
                        var entry = archive.CreateEntry(fileName);   
                        using var entryStream = entry.Open();
                        entryStream.Write(pdf, 0, pdf.Length);
                    }
                }

                return File(zipStream.ToArray(), "application/zip", "Statements.zip");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error generating statement(s): " + ex.Message);
            }
        }
    }
}
