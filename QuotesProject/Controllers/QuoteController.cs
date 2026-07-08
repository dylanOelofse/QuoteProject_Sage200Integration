using Microsoft.AspNetCore.Mvc;
using QuotesProject.Interfaces;
using QuotesProject.Models;
using System.Diagnostics;

namespace QuotesProject.Controllers
{
    [Route("[controller]")]
    public class QuoteController : Controller
    {
        private readonly IQuoteService _quoteService;

        public QuoteController(IQuoteService quoteService)
        {
            _quoteService = quoteService;
        }

        [HttpGet("/")]
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
                var viewModel = new QuoteViewModel { Quotes = new List<Quote> { quote } };
                return View(viewModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public IActionResult CreateQuote([FromBody] Quote quote)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _quoteService.CreateQuote(quote);
                return Ok(quote);
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
        public IActionResult UpdateQuote([FromBody] Quote quote)
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
    }
}
