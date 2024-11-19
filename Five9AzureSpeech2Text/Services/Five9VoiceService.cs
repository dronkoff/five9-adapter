using Five9.Voicestream;
using Grpc.Core;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;

namespace Five9AzureSpeech2Text.Services
{
    public class Five9VoiceService : Voice.VoiceBase
    {
        private readonly ILogger<Five9VoiceService> _logger;
        private readonly IConfiguration _config;
        public Five9VoiceService(IConfiguration config, ILogger<Five9VoiceService> logger)
        {
            _logger = logger;
            _config = config;
        }

        public override async Task StreamingVoice(IAsyncStreamReader<StreamingVoiceRequest> requestStream, IServerStreamWriter<StreamingVoiceResponse> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("SRV: BEGIN StreamingVoice");

            // PROTO: The first message must be 'streaming_config' containing control data specific to the call being streamed.
            // PROTO: After sending the 'streaming_config' message, the client must wait for a response from the server with status code SRV_START_STREAMING before sending audio payloads.
            if (await requestStream.MoveNext(context.CancellationToken))
            {
                if (requestStream.Current.StreamingConfig != null)
                {
                    _logger.LogInformation($"StreamingConfig.VccCallId: {requestStream.Current.StreamingConfig.VccCallId}");
                    _logger.LogInformation($"StreamingConfig.Encoding: {requestStream.Current.StreamingConfig.AgentId}");
                    _logger.LogInformation($"StreamingConfig.VoiceConfig.Encoding: {requestStream.Current.StreamingConfig.VoiceConfig.Encoding}");
                }
                else
                {
                    _logger.LogInformation("requestStream.StreamingConfig is required as a first message");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "requestStream.StreamingConfig is required as a first message."));
                }
            }

            // https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-recognize-speech?pivots=programming-language-csharp
            // using vd-speech from AOAI-Test for test
            //var speechConfig = SpeechConfig.FromEndpoint(new Uri(_config["AZSpeechEndpoint"]), _config["AZSpeechKey"]);// .FromSubscription(_config["AZSpeechKey"], _config["AZSpeechRegion"]);
            var speechConfig = SpeechConfig.FromSubscription(_config["AZSpeechKey"], "eastus");
            using var audioConfigStream = AudioInputStream.CreatePushStream();
            using var audioConfig = AudioConfig.FromStreamInput(audioConfigStream);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var stopRecognition = new TaskCompletionSource<int>();
            SubsribeToRecognizerEvents(recognizer, stopRecognition);

            await recognizer.StartContinuousRecognitionAsync();

            // signal the client to start streaming
            await responseStream.WriteAsync(new StreamingVoiceResponse { 
                Status = new StreamingStatus { Code = StreamingStatus.Types.StatusCode.SrvReqStartStreaming }
            });

            while (await requestStream.MoveNext(context.CancellationToken))
            {
                // PROTO: The subsequent messages must be 'audio_content' with audio payload.
                if (requestStream.Current.AudioContent != null)
                {
                    var buffer = requestStream.Current.AudioContent.ToByteArray();
                    _logger.LogInformation("SRV: Bytes received: {0} ({1}...)", buffer.Length, Convert.ToBase64String(buffer.AsSpan(0, Math.Min(buffer.Length, 10))));
                    audioConfigStream.Write(buffer, buffer.Length);
                }

                // PROTO: Optionally status messages 'streaming_status' can be sent any time to provide additional information e.g events, notifications about the call.
                if(requestStream.Current.StreamingStatus != null)
                {
                    /* Client status codes
                        CLT_CALL_ENDED = 1;                 // Call ended. Close the gRPC channel.
                        CLT_CALL_HOLD = 2;
                        CLT_CALL_RESUME = 3;
                        CLT_DISCONNECT = 4;                 // Client closing gRPC channel.
                        CLT_ERROR_NO_RESOURCE = 100;
                        CLT_ERROR_TIMEOUT = 101;
                        CLT_ERROR_GENERIC = 102;
                    */
                    _logger.LogInformation("SRV: StreamingStatus received {0}", requestStream.Current.StreamingStatus);
                    if (requestStream.Current.StreamingStatus.Code == StreamingStatus.Types.StatusCode.CltDisconnect || 
                        requestStream.Current.StreamingStatus.Code == StreamingStatus.Types.StatusCode.CltCallEnded)
                    {
                        break;
                    }
                }
            }

            await recognizer.StopContinuousRecognitionAsync();

            _logger.LogInformation("SRV: END StreamingVoice");
            // signal the client that we are done
            await responseStream.WriteAsync(new StreamingVoiceResponse { 
                Status = new StreamingStatus { Code = StreamingStatus.Types.StatusCode.SrvReqDisconnect }
            });

            //var speechRecognitionResult = await recognizer.RecognizeOnceAsync();
            //_logger.LogInformation($"RECOGNIZED: Text={speechRecognitionResult.Text}");

            _logger.LogInformation("SRV: END StreamingVoice");

            //await base.StreamingVoice(requestStream, responseStream, context);
        }

        private void SubsribeToRecognizerEvents(SpeechRecognizer recognizer, TaskCompletionSource<int> stopRecognition)
        {
            recognizer.Recognizing += (s, e) =>
            {
                _logger.LogInformation($"RECOGNIZING: Text={e.Result.Text}");
            };

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    _logger.LogInformation($"RECOGNIZED: Text={e.Result.Text}");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    _logger.LogInformation($"NOMATCH: Speech could not be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                _logger.LogInformation($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    _logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode}");
                    _logger.LogInformation($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    _logger.LogInformation($"CANCELED: Did you set the speech resource key and region values?");
                }

                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                _logger.LogInformation("\n    Session stopped event.");
                stopRecognition.TrySetResult(0);
            };
        }


    }
}
