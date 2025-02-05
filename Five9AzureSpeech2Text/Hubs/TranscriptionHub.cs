using Microsoft.AspNetCore.SignalR;

namespace Five9AzureSpeech2Text.Hubs
{
    public class TranscriptionHub: Hub<ITranscriptionHub>
    {
        public async Task Recognizing(string vccCallId, string text)
        {
            await Clients.All.Recognizing(text);
        }
        public async Task Recognized(string vccCallId, string text)
        {
            await Clients.All.Recognized(text);
        }
    }
}
