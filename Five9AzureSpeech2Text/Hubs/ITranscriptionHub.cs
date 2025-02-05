namespace Five9AzureSpeech2Text.Hubs
{
    public interface ITranscriptionHub
    {
        Task Recognizing(string text);
        Task Recognized(string text);
    }
}
