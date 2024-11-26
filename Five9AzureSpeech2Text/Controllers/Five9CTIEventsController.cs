using Five9AzureSpeech2Text.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Five9AzureSpeech2Text.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Five9CTIEventsController : ControllerBase
    {
        private readonly ILogger<Five9CTIEventsController> _logger;
        private readonly IConfiguration _config;

        public Five9CTIEventsController(IConfiguration config, ILogger<Five9CTIEventsController> logger)
        {
            _config = config;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Get called. Responding with token.");
            var trustToken = _config["Five9TrustToken"] ?? throw new Exception("Five9TrustToken is not configured.");
            return Content(trustToken, "application/json");
        }

        [HttpPost]
        public IActionResult Post([FromBody] dynamic jsonData)
        {
            string s = jsonData.description;
            _logger.LogInformation("Post called. Probably a CTI event. {0}", s);
            return Ok();
        }

        //[HttpPost("GetSpeechToText")]
        //public IActionResult GetSpeechToText([FromBody] IFormFile file)
        //{
        //    return Ok("GetSpeechToText");
        //}
    }
}
