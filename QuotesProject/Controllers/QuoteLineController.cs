using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuotesProject.Interfaces;
using QuotesProject.Models;
using QuotesProject.Services;

namespace QuotesProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
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
        public IActionResult Index()
        {
            int? quoteId = HttpContext.Session.GetInt32("OpenQuoteId");

            if (quoteId is null)                                // session expired, or landed here directly
                return RedirectToAction("Index", "Quote");

            try
            {
                QuoteLineViewModel model = new QuoteLineViewModel
                {
                    quoteOpened = _quoteService.GetQuoteById(quoteId.Value),
                    quoteLines = _quoteLineService.GetQuoteLines(quoteId.Value)
                };

                return View(model);
            }
            catch (KeyNotFoundException)
            {
                HttpContext.Session.Remove("OpenQuoteId");      // id no longer resolves - drop it so we don't loop back here
                return RedirectToAction("Index", "Quote");
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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