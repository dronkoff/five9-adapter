//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//********************************************************* 

using Five9AzureSpeech2Text.Hubs;
using Five9AzureSpeech2Text.Services;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "TranscriptionHubCorsPolicy",
                      policy =>
                      {
                          //policy.WithOrigins("http://example.com");
                          policy.WithOrigins("https://localhost:7042");
                          //policy.WithOrigins("https://Five9SpeechClient");
                          //policy.WithMethods("GET", "POST");
                          policy.AllowAnyMethod(); // should allow specific methods in reality
                          policy.AllowAnyHeader();
                          policy.AllowCredentials();
                      });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<Five9VoiceService>();
app.MapControllers();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.UseCors("TranscriptionHubCorsPolicy");
app.MapHub<TranscriptionHub>("/transcriptionhub");


app.Run();
