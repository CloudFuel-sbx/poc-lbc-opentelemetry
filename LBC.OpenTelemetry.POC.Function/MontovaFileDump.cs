using LBC.OpenTelemetry.POC.Function.Constants;
using LBC.OpenTelemetry.POC.Function.Refit;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LBC.OpenTelemetry.POC.Function;

public class MontovaFileDump
{
    private readonly ILogger<MontovaFileDump> _logger;
    private readonly INominationApi _nominationApi;

    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    public MontovaFileDump(ILogger<MontovaFileDump> logger, INominationApi nominationApi)
    {
        _logger = logger;
        _nominationApi = nominationApi;
    }

    [Function(nameof(MontovaFileDump))]
    public async Task Run([BlobTrigger("montova/{name}", Connection = "StorageAccountConnection")] Stream stream, string name, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Current Activity: {Activity.Current?.TraceId}");

        // Create activity - will be logged as INTERNAL in Application Insights
        using var activity = ActivitySource.StartActivity(nameof(MontovaFileDump));

        activity!.AddEvent(new($"Blob trigger, read file with name {name}"));
        activity!.SetTag("montova.file.name", name);

        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync(cancellationToken);

        activity!.SetTag("montova.file.content", content);

        _logger.LogInformation("Just logging some information.");

        // Track nested activities
        ExecuteFileScan();

        // Add Event - will be logged as EVENT in Application Insights
        activity!.AddEvent(new("Gonna send API request"));

        await _nominationApi.CreateNominationAsync(new(name), cancellationToken);

        activity!.SetStatus(ActivityStatusCode.Ok, "Successfully sent API request.");
    }

    private static void ExecuteFileScan()
    {
        using var childActivity = ActivitySource.StartActivity("FileScan");
        childActivity!.AddTag("operation.name", nameof(ExecuteFileScan));
    }
}