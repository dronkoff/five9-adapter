using Microsoft.AspNetCore.SignalR;

namespace Five9AzureSpeech2Text.Hubs
{
    public class TranscriptionHub: Hub<ITranscriptionHub>
    {
        public async Task RegisterForTranscript(string vccCallId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, vccCallId);
        }

        public async Task Recognizing(string vccCallId, string text)
        {
            await Clients.Group(vccCallId).Recognizing(text);
        }

        public async Task Recognized(string vccCallId, string text)
        {
            await Clients.Group(vccCallId).Recognized(text);
        }
    }
}
