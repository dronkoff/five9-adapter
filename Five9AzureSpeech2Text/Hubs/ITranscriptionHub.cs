namespace Five9AzureSpeech2Text.Hubs
{
    public interface ITranscriptionHub
    {
        Task RegisterForTranscript(string vccCallId);
        Task Recognizing(string text);
        Task Recognized(string text, long offsetInTicks, string speakerId);
    }
}
