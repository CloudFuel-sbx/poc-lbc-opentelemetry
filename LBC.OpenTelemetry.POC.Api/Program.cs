using Azure.Monitor.OpenTelemetry.AspNetCore;
using LBC.OpenTelemetry.POC.Api.Constants;
using LBC.OpenTelemetry.POC.Api.Providers;
using LBC.OpenTelemetry.POC.Api.ServiceBus;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Endpoint = NServiceBus.Endpoint;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IServiceBusService, ServiceBusService>();

var endpointConfiguration = new EndpointConfiguration(TelemetryConstants.ServiceName);
endpointConfiguration.UseTransport(new AzureServiceBusTransport("sbns-lbctt-d-observability-poc.servicebus.windows.net", CredentialProvider.GetCredentials()));
endpointConfiguration.EnableOpenTelemetry();
endpointConfiguration.SendOnly();

var endpointInstance = await Endpoint.Start(endpointConfiguration);

// Voeg IMessageSession toe aan de DI-container
builder.Services.AddSingleton<IMessageSession>(endpointInstance);

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("sbns-lbctt-d-observability-poc"))
        .AddSource("NServiceBus.*");
});

builder.Services.AddOpenTelemetry()
        .WithTracing(builder =>
        {
            builder
                .AddSource(TelemetryConstants.ServiceName)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(TelemetryConstants.ServiceName));
        })
        .UseAzureMonitor(x =>
        {
            x.ConnectionString = "InstrumentationKey=683f5d2f-c075-45b8-86a0-1906ac8af86d;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/";
        });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

app.Lifetime.ApplicationStopping.Register(async () =>
{
    await endpointInstance.Stop();
});