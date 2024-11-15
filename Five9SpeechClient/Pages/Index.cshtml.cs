using Five9AzureSpeech2TextClient;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Five9SpeechClient.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    public string Message { get; set; }
    
    private readonly Greeter.GreeterClient _greeterClient;

    public IndexModel(Greeter.GreeterClient greeterClient, ILogger<IndexModel> logger)
    {
        _logger = logger;
        _greeterClient = greeterClient;
    }

    public async Task OnGet()
    {
        var reply = await _greeterClient.SayHelloAsync(new HelloRequest
        {
            Name = "gRPC web client"
        });
        this.Message = reply.Message;
    }
}
