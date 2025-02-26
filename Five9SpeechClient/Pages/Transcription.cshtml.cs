using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Five9SpeechClient.Pages
{
    public class TranscriptionModel : PageModel
    {
        [BindProperty]
        public string? Five9AdapterUrl { get; set; }

        public TranscriptionModel(IConfiguration configuration)
        {
            Five9AdapterUrl = configuration["services:five9azurespeech2text:https:0"]; // .NET Aspire naming convention
        }

        [BindProperty(SupportsGet = true)]
        public string? CallId { get; set; }

        public void OnGet()
        {
        }
    }
}
