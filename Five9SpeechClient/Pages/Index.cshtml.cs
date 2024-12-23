using Five9.Voicestream;
using Five9AzureSpeech2TextClient;
using Five9SpeechClient.Helpers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace Five9SpeechClient.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    public string Message { get; set; }
    [BindProperty]
    public IFormFile UploadedFile { get; set; }
    
    private readonly Greeter.GreeterClient _greeterClient;
    private readonly Voice.VoiceClient _five9VoiceClient;

    public IndexModel(Greeter.GreeterClient greeterClient, Voice.VoiceClient five9VoiceClient, ILogger<IndexModel> logger)
    {
        _logger = logger;
        _greeterClient = greeterClient;
        _five9VoiceClient = five9VoiceClient;
    }

    public async Task OnGet()
    {
        var reply = await _greeterClient.SayHelloAsync(new HelloRequest
        {
            Name = "gRPC web client"
        });
        this.Message = reply.Message;
    }

    /// <summary></summary>
    /// <seealso cref="https://documentation.five9.com/en-us/assets/documentation/voicestream/voicestream-dev-guide.pdf"/>
    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            ModelState.AddModelError("UploadedFile", "File is required.");
            return Page();
        }

        if (UploadedFile == null || UploadedFile.Length < 44)
        {
            ModelState.AddModelError("UploadedFile", "File too small for a wav file.");
            return Page();
        }

        if (!String.Equals(Path.GetExtension(UploadedFile.FileName), ".wav", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("UploadedFile", "The upload should be a *.wav file.");
            return Page();
        };

        _logger.LogInformation("Uploading file {FileName} ({Length} bytes)", UploadedFile.FileName, UploadedFile.Length);

        byte[] wavHeaderBytes = new byte[44];
        using (var stream = UploadedFile.OpenReadStream())
        {
            var bytesRead = await stream.ReadAsync(wavHeaderBytes, 0, wavHeaderBytes.Length);
            if(bytesRead < wavHeaderBytes.Length)
            {
                ModelState.AddModelError("UploadedFile", "Failed to read wav file header.");
                return Page();
            }
        }
        var wavHeader = new WavHeaderReader(wavHeaderBytes);

        if(wavHeader.GetChannels() != 1)
        {
            ModelState.AddModelError("UploadedFile", "The audio file must be mono.");
            return Page();
        }

        if(wavHeader.GetBitsPerSample() != 16)
        {
            ModelState.AddModelError("UploadedFile", "The audio file must be 16-bit.");
            return Page();
        }

        using var duplexCall = _five9VoiceClient.StreamingVoice();
        var (requestStream, responseStream) = (duplexCall.RequestStream, duplexCall.ResponseStream);

        // PROTO: Multiple 'StreamingVoiceRequest' messages are sent repeatedly as defined below.
        // PROTO: The first message must be 'streaming_config' containing control data specific to the call being streamed.
        var voiceConfig = new VoiceConfig()
        {
            Encoding = wavHeader.GetAudioFormat() switch
            {
                1 => VoiceConfig.Types.AudioEncoding.Linear16,
                7 => VoiceConfig.Types.AudioEncoding.Mulaw,
                _ => throw new ArgumentException($"Audio format from file header {wavHeader.GetAudioFormat()} is not supported. Should 1 for PCM and 7 fro Mulaw.")
            },
            SampleRateHertz = wavHeader.GetSampleRate()
        };

        await requestStream.WriteAsync(new StreamingVoiceRequest()
        {
            StreamingConfig = new StreamingConfig()
            {
                VoiceConfig = voiceConfig,
                VccCallId = "123",
                DomainId = "ABC",
                CampaignId = "XYZ",
                AgentId = "456",
                CallLeg = StreamingConfig.Types.CallLeg.Agent,
                TrustToken = "ASDFadsf12341asfasd",
                SubscriptionId = "789",
                SkillId = "0101010",
            },
            SendTime = Timestamp.FromDateTime(DateTime.UtcNow.ToUniversalTime())
        });

        // PROTO: After sending the 'streaming_config' message, the client must wait for a response from the server with status code SRV_START_STREAMING before sending audio payloads.
        if (!await responseStream.MoveNext(CancellationToken.None))
        {
            var err = "The server did not respond after the first streaming_config message.";
            _logger.LogError(err);
            ModelState.AddModelError("UploadedFile", err);
            return Page();
        }
        if (responseStream.Current.Status.Code != StreamingStatus.Types.StatusCode.SrvReqStartStreaming)
        {
            var err = "The first server response is not the SRV_START_STREAMING response.";
            _logger.LogError(err);
            ModelState.AddModelError("UploadedFile", err);
            return Page();
        }

        // PROTO: The subsequent messages must be 'audio_content' with audio payload.
        using (var stream = UploadedFile.OpenReadStream())
        {
            byte[] buffer = new byte[8192]; // 8KB buffer

            int bytesRead;
            _logger.LogInformation("CLEINT: STREAM START");
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await requestStream.WriteAsync(new StreamingVoiceRequest()
                {
                    AudioContent = ByteString.CopyFrom(buffer, 0, bytesRead),
                    SendTime = Timestamp.FromDateTime(DateTime.UtcNow.ToUniversalTime())
                });
            }
            _logger.LogInformation("CLEINT: STREAM END");
            await requestStream.WriteAsync(new StreamingVoiceRequest()
            {
                StreamingStatus = new StreamingStatus()
                {
                    Code = StreamingStatus.Types.StatusCode.CltCallEnded
                },
                SendTime = Timestamp.FromDateTime(DateTime.UtcNow.ToUniversalTime())
            });
        }

        this.Message = $"Uploaded {UploadedFile.FileName} ({UploadedFile.Length} bytes)\n";

        // wait for the server to finish processing
        if (!await responseStream.MoveNext(CancellationToken.None))
        {
            var err = "The server did not respond after the last audio_content message.";
            _logger.LogError(err);
            ModelState.AddModelError("UploadedFile", err);
            return Page();
        }
        _logger.LogInformation($"Server response is {responseStream.Current.Status.Code}");
        this.Message += $"Server response is {responseStream.Current.Status.Code} \n";
        await requestStream.CompleteAsync();


        return Page();
    }

}
