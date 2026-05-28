using Microsoft.AspNetCore.Mvc;
using WebAppTemplate1.Models;
using WebAppTemplate1.Services;

namespace WebAppTemplate1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultationController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ConsultationController> _logger;

        public ConsultationController(IEmailService emailService, ILogger<ConsultationController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookConsultation([FromBody] ConsultationBooking booking)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Send confirmation email
                await _emailService.SendConsultationConfirmationAsync(booking);

                _logger.LogInformation($"Consultation booked for {booking.FullName} on {booking.ConsultationDate:yyyy-MM-dd} at {booking.ConsultationTime}");

                return Ok(new { success = true, message = "Consultation booked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking consultation");
                return StatusCode(500, new { success = false, message = "Failed to book consultation. Please try again." });
            }
        }
    }
}
