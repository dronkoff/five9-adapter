using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using static System.Net.Mime.MediaTypeNames;
using System.Text;

namespace SpeechTests
{
    [TestClass]
    public class IntegrationTest1
    {
        private IConfiguration _config;

        [TestInitialize]
        public void InitClass()
        {
            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddUserSecrets<IntegrationTest1>()
                .AddEnvironmentVariables();
            _config = builder.Build();
        }

        // Instructions:
        // 1. Add a project reference to the target AppHost project, e.g.:
        //
        //    <ItemGroup>
        //        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
        //    </ItemGroup>
        //
        // 2. Uncomment the following example test and update 'Projects.MyAspireApp_AppHost' to match your AppHost project:
        //
        // [TestMethod]
        // public async Task GetWebResourceRootReturnsOkStatusCode()
        // {
        //     // Arrange
        //     var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyAspireApp_AppHost>();
        //     appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        //     {
        //         clientBuilder.AddStandardResilienceHandler();
        //     });
        //     await using var app = await appHost.BuildAsync();
        //     var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        //     await app.StartAsync();

        //     // Act
        //     var httpClient = app.CreateHttpClient("webfrontend");
        //     await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        //     var response = await httpClient.GetAsync("/");

        //     // Assert
        //     Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        // }

        [TestMethod]
        public async Task SendWAVToSpeachService()
        {
            var sk = _config["AZSpeechKey"];
            var speechConfig = SpeechConfig.FromSubscription(sk, "eastus");
            //using var audioConfig = AudioConfig.FromWavFileInput("C:\\SRC\\WorksafeBC\\Five9AzureAdapter\\doc\\COM_Closed_HighVolume2.wav");
            using var audioConfig = AudioConfig.FromWavFileInput("C:\\SRC\\WorksafeBC\\Five9AzureAdapter\\doc\\test-call.wav");
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var speechRecognitionResult = await recognizer.RecognizeOnceAsync();
            Assert.IsNotNull(speechRecognitionResult);
            Assert.IsNotNull(speechRecognitionResult.Text);
            Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
        }


        [TestMethod]
        public async Task SendByteArrayToSpeachService()
        {
            var sk = _config["AZSpeechKey"];
            var speechConfig = SpeechConfig.FromSubscription(sk, "eastus");
            speechConfig.SpeechRecognitionLanguage = "en-US";
            //using var audioConfig = AudioConfig.FromWavFileInput("C:\\Temp\\_The-quick-brown-fox.wav");

            // var audioFormat = AudioStreamFormat.GetWaveFormat(44100, 32, 2, AudioStreamWaveFormat.G722);
            // using var reader = new BinaryReader(File.OpenRead("C:\\Temp\\_Portuguese_man_o_war.wav"));

            var audioFormat = AudioStreamFormat.GetWaveFormat(8000, 16, 1, AudioStreamWaveFormat.MULAW);
            using var reader = new BinaryReader(File.OpenRead("C:\\SRC\\WorksafeBC\\Five9AzureAdapter\\doc\\test-call.wav"));
            // var audioFormat = AudioStreamFormat.GetWaveFormat(8000, 16, 1, AudioStreamWaveFormat.PCM);
            // using var reader = new BinaryReader(File.OpenRead("C:\\SRC\\WorksafeBC\\Five9AzureAdapter\\doc\\COM_Closed_HighVolume2.wav"));

            // Using a push stream as input assumes that the audio data is raw PCM and skips any headers.
            // The API still works in certain cases if the header isn't skipped. For the best results, consider
            // implementing logic to read off the headers so that byte[] begins at the start of the audio data.
            // http://soundfile.sapp.org/doc/WaveFormat/
            // reader.BaseStream.Seek(44, SeekOrigin.Begin); // Skip the WAV header (typically 44 bytes)

            using var audioInputStream = AudioInputStream.CreatePushStream(audioFormat);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            Console.WriteLine("Starting recognition");
            var sb = new StringBuilder();
            var stopRecognition = new TaskCompletionSource<int>();
            var startRecognition = new TaskCompletionSource<int>();
            SubsribeToRecognizerEvents(recognizer, stopRecognition, startRecognition, sb);

            await recognizer.StartContinuousRecognitionAsync();

            await startRecognition.Task;
            Console.WriteLine("Recognission started. Sending bytes.");

            byte[] readBytes;
            do
            {
                readBytes = reader.ReadBytes(1024);
                //Console.WriteLine(string.Join(", ", readBytes.Select(b => (int)b)));
                audioInputStream.Write(readBytes, readBytes.Length);
            } while (readBytes.Length > 0);

            Console.WriteLine("Finished sending bytes");

            //Task.WaitAny([stopRecognition.Task]);
            await stopRecognition.Task;

            await recognizer.StopContinuousRecognitionAsync();

            Console.WriteLine("After stopRecognition await");

            Console.WriteLine(sb.ToString());
            //var speechRecognitionResult = await recognizer.RecognizeOnceAsync();
            //Assert.IsNotNull(speechRecognitionResult);
            //Assert.IsNotNull(speechRecognitionResult.Text);
            //Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
        }

        private void SubsribeToRecognizerEvents(SpeechRecognizer recognizer, TaskCompletionSource<int> stopRecognition, TaskCompletionSource<int> startRecognition, StringBuilder sb)
        {
            recognizer.SessionStarted += (s, e) =>
            {
                //Console.WriteLine("Session started event.");
                sb.AppendLine("Session started event.");
                startRecognition.TrySetResult(0);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                //Console.WriteLine("Session stopped event.");
                sb.AppendLine("Session stopped event.");
                stopRecognition.TrySetResult(0);
            };

            recognizer.SpeechStartDetected += (s, e) =>
            {
                //Console.WriteLine("Speech start detected event.");
                sb.AppendLine("Speech start detected event.");
            };

            recognizer.SpeechEndDetected += (s, e) =>
            {
                //Console.WriteLine("Speech end detected event.");
                sb.AppendLine("Speech end detected event.");
            };

            recognizer.Recognizing += (s, e) =>
            {
                //Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                sb.AppendLine($"RECOGNIZING: Text={e.Result.Text}");
            };

            recognizer.Recognized += (s, e) =>
            {
                //Console.WriteLine($"RECOGNIZED: Reason={e.Result.Reason}, Text={e.Result.Text}");
                sb.AppendLine($"RECOGNIZED: Reason={e.Result.Reason}, Text={e.Result.Text}");
            };

            recognizer.Canceled += (s, e) =>
            {
                //Console.WriteLine($"CANCELED: Reason={e.Reason}");
                sb.AppendLine($"CANCELED: Reason={e.Reason}");
                if (e.Reason == CancellationReason.Error)
                {
                    sb.AppendLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    sb.AppendLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    sb.AppendLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                stopRecognition.TrySetResult(0);
            };
        }
    }
}
