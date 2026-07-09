using Microsoft.AspNetCore.Mvc;
using QuotesProject.Interfaces;
using QuotesProject.Models;
using QuotesProject.Services;

namespace QuotesProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuoteLineController : Controller
    {
        private readonly IQuoteService _quoteService;
        private readonly IQuoteLineService _quoteLineService;

        public QuoteLineController(IQuoteService quoteService, IQuoteLineService quoteLineService)
        {
            _quoteService = quoteService;
            _quoteLineService = quoteLineService;
        }

        [HttpGet]
        public IActionResult Index(int quoteId)
        {
            try
            {
                QuoteLineViewModel model = new QuoteLineViewModel
                {
                    quoteOpened = _quoteService.GetQuoteById(quoteId),
                    quoteLines = _quoteLineService.GetQuoteLines(quoteId)
                };

                return View(model);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error loading quote: " + ex.Message);
            }
        }

        [HttpGet("{lineId}")]
        public IActionResult GetQuoteLineById(int lineId)
        {
            try
            {
                var quoteLine = _quoteLineService.GetQuoteLineById(lineId);
                return Ok(quoteLine);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote line not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving quote line: " + ex.Message);
            }
        }

        [HttpPost]
        public IActionResult CreateQuoteLine(QuoteLine quoteLine)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _quoteLineService.CreateQuoteLine(quoteLine);
                return Ok(quoteLine);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error creating quote line: " + ex.Message);
            }
        }

        [HttpPut]
        public IActionResult UpdateQuoteLine(QuoteLine quoteLine)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _quoteLineService.UpdateQuoteLine(quoteLine);
                return RedirectToAction("Index");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote line not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating quote line: " + ex.Message);
            }
        }

        [HttpDelete("{lineId}")]
        public IActionResult DeleteQuoteLine(int lineId)
        {
            try
            {
                _quoteLineService.DeleteQuoteLine(lineId);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quote line not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error deleting quote line: " + ex.Message);
            }
        }
    }
}