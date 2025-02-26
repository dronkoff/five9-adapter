using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Five9SpeechClient.Pages
{
    public class TranscriptionModel : PageModel
    {

        [BindProperty(SupportsGet = true)]
        public string? CallId { get; set; }

        public void OnGet()
        {
        }
    }
}
