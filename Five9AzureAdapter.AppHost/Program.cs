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

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var speechAdapterApi = builder.AddProject<Projects.Five9AzureSpeech2Text>("five9azurespeech2text");

builder.AddProject<Projects.Five9SpeechClient>("five9speechclient")
    .WithReference(speechAdapterApi);

builder.Build().Run();
