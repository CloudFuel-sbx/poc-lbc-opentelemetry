using LBC.OpenTelemetry.POC.Function.Constants;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LBC.OpenTelemetry.POC.Function
{
    public class TrackTraceRequestBody
    {
        [JsonPropertyName("TraceId")]
        public string TraceId { get; set; }

        [JsonPropertyName("SpanId")]
        public string SpanId { get; set; }

        [JsonPropertyName("ActivityName")]
        public string ActivityName { get; set; }

        [JsonPropertyName("Message")]
        public string Message { get; set; }

        [JsonPropertyName("LogSeverity")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogSeverity LogSeverity { get; set; }
    }

    public enum LogSeverity
    {
        Information,
        Warning,
        Error
    }

    public class TrackTrace
    {
        private readonly ILogger _logger;

        private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

        public TrackTrace(ILogger<TrackTrace> logger)
        {
            _logger = logger;
        }

        [Function("TrackTrace")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "TrackTrace")] HttpRequestData request, CancellationToken cancellationToken)
        {
            try
            {
                var trackTraceRequest = await JsonSerializer.DeserializeAsync<TrackTraceRequestBody>(request.Body, cancellationToken: cancellationToken);

                if (trackTraceRequest != null && !string.IsNullOrEmpty(trackTraceRequest.TraceId))
                {
                    ActivityContext parentContext;

                    if (!string.IsNullOrEmpty(trackTraceRequest.SpanId))
                    {
                        parentContext = new ActivityContext(ActivityTraceId.CreateFromString(trackTraceRequest.TraceId.AsSpan()), ActivitySpanId.CreateFromString(trackTraceRequest.SpanId.AsSpan()), ActivityTraceFlags.Recorded);
                    }
                    else
                    {
                        parentContext = new ActivityContext(ActivityTraceId.CreateFromString(trackTraceRequest.TraceId.AsSpan()), default, ActivityTraceFlags.Recorded);
                    }

                    if (parentContext == default)
                        throw new InvalidOperationException("Please send a TraceId or SpanId in the request body.");

                    using var activity = ActivitySource.StartActivity(trackTraceRequest.ActivityName, ActivityKind.Server, parentContext);

                    switch (trackTraceRequest.LogSeverity)
                    {
                        case LogSeverity.Information:
                            _logger.LogInformation("Information: {Message}", trackTraceRequest.Message); break;
                        case LogSeverity.Warning:
                            _logger.LogWarning("Warning: {Message}", trackTraceRequest.Message); break;
                        case LogSeverity.Error:
                            _logger.LogError("Error: {Message}", trackTraceRequest.Message); break;
                    }

                    var response = request.CreateResponse(HttpStatusCode.OK);
                    await response.WriteStringAsync(activity.SpanId.ToString());
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deserializing request body: {ex.Message}.");
            }

            var badRequestResponse = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteStringAsync("Invalid request data.");
            return badRequestResponse;
        }
    }
}