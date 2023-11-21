using Azure.Monitor.OpenTelemetry.AspNetCore;
using LBC.OpenTelemetry.POC.Function.Constants;
using LBC.OpenTelemetry.POC.Function.Refit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Refit;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddLogging();

        services.AddOpenTelemetry()
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

        services.AddScoped(typeof(HttpLoggingHandler<>));

        services.AddRefitClient<INominationApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri("https://apim-lbctt-d-observability-poc.azure-api.net");
                    //c.BaseAddress = new Uri("https://localhost:7190");
                })
                .AddHttpMessageHandler<HttpLoggingHandler<INominationApi>>();
    })
    .Build();

host.Run();