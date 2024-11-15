using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var speechAdapterApi = builder.AddProject<Projects.Five9AzureSpeech2Text>("five9azurespeech2text");

builder.AddProject<Projects.Five9SpeechClient>("five9speechclient")
    .WithReference(speechAdapterApi);

builder.Build().Run();
