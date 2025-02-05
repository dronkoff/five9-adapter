using Microsoft.AspNetCore.SignalR;

namespace Five9AzureSpeech2Text.Hubs
{
    public class TranscriptionHub: Hub
    {
        public async Task TranscriptionEvent(string eventName, string text)
        {
            await Clients.All.SendAsync("TranscriptionEvent", eventName, text);
        }
    }
}
