using Azure.Messaging.ServiceBus;
using LBC.OpenTelemetry.POC.Api.Constants;
using LBC.OpenTelemetry.POC.Api.Providers;
using System.Diagnostics;

namespace LBC.OpenTelemetry.POC.Api.ServiceBus;

public interface IServiceBusService
{
    Task SendMessageAsync(ServiceBusMessageBuilder serviceBusMessageBuilder, CancellationToken cancellationToken = default);
}

internal class ServiceBusService : IServiceBusService
{
    private ServiceBusClient _serviceBusClient;
    private readonly ILogger<ServiceBusService> _logger;

    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    public ServiceBusService(ILogger<ServiceBusService> logger)
    {
        _serviceBusClient = new ServiceBusClient("sbns-lbctt-d-observability-poc.servicebus.windows.net", CredentialProvider.GetCredentials());

        _logger = logger;
    }

    public async Task SendMessageAsync(ServiceBusMessageBuilder serviceBusMessageBuilder, CancellationToken cancellationToken = default)
    {
        var serviceBusSender = _serviceBusClient.CreateSender(serviceBusMessageBuilder.QueueOrTopicName);
        var message = serviceBusMessageBuilder.Build();

        var parentSpan = Activity.Current;

        var sendSbMessageEvent = parentSpan?.AddEvent(new("Send ServiceBus message"));
        sendSbMessageEvent?.AddTag("message.id", message.MessageId);
        sendSbMessageEvent?.AddTag("message.body", message.Body);
        sendSbMessageEvent?.AddTag("message.subject", message.Subject);
        sendSbMessageEvent?.AddTag("message.correlationid", message.CorrelationId);

        await serviceBusSender.SendMessageAsync(message, cancellationToken);

        _logger.LogInformation("Sent message {MessageId} to {QueueOrTopicName}", message.MessageId, serviceBusMessageBuilder.QueueOrTopicName);
    }
}