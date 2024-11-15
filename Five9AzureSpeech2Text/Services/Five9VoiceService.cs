using Five9.Voicestream;
using Grpc.Core;

namespace Five9AzureSpeech2Text.Services
{
    public class Five9VoiceService : Voice.VoiceBase
    {
        private readonly ILogger<GreeterService> _logger;
        public Five9VoiceService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task StreamingVoice(IAsyncStreamReader<StreamingVoiceRequest> requestStream, IServerStreamWriter<StreamingVoiceResponse> responseStream, ServerCallContext context)
        {
            return base.StreamingVoice(requestStream, responseStream, context);
        }



    }
}
