using LBC.OpenTelemetry.POC.Api.Constants;
using LBC.OpenTelemetry.POC.Api.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LBC.OpenTelemetry.POC.Api.Controllers;

public record CreateNomination(string FileName);

public record CreateNominationMessageNServiceBus(Guid Id, string FileName) : IMessage;

[ApiController]
[Route("api/[controller]")]
public class NominationController : ControllerBase
{
    private readonly IServiceBusService _serviceBusService;
    private readonly ILogger<NominationController> _logger;
    private readonly IMessageSession _messageSession;

    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    public NominationController(ILogger<NominationController> logger, IServiceBusService serviceBusService, IMessageSession messageSession)
    {
        _logger = logger;
        _serviceBusService = serviceBusService;
        _messageSession = messageSession;
    }

    [HttpPost]
    public async Task PostAsync(CreateNomination createNominationRequest, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Received CreateNomination request for file {createNominationRequest.FileName}");

        var parentSpan = Activity.Current;

        Activity activity;

        if (parentSpan != null)
        {
            activity = ActivitySource.StartActivity(ActivityKind.Internal, parentSpan.Context, name: "Send ServiceBus message");
        }
        else
        {
            activity = ActivitySource.StartActivity("Send ServiceBus message");
        }

        var messageId = Guid.NewGuid();

        var sendSbMessageEvent = activity?.AddEvent(new("Send ServiceBus message"));
        sendSbMessageEvent?.AddTag("message.id", activity);
        sendSbMessageEvent?.AddTag("parentSpan.id", activity.Id);
        sendSbMessageEvent?.AddTag("parentSpan.spanid", activity.SpanId);
        sendSbMessageEvent?.AddTag("parentSpan.traceid", activity.TraceId);

        var sendOptions = new SendOptions();
        sendOptions.SetDestination("sbq-nominations");
        sendOptions.SetHeader("x-ms-client-tracking-id", activity.TraceId.ToString());
        sendOptions.SetHeader("Diagnostic-Id", activity.TraceId.ToString());
        sendOptions.SetHeader("CorrelationId", activity.TraceId.ToString());
        sendOptions.SetHeader("ActivityId", activity.Id.ToString());
        sendOptions.SetHeader("ActivityParentId", activity.ParentId.ToString());
        sendOptions.SetHeader("x-ms-client-tracking-id", activity.Id.ToString());

        sendOptions.StartNewConversation(activity.Id.ToString());

        sendOptions.SetMessageId(activity.Id.ToString());

        //var builder = new ServiceBusMessageBuilder("FromController", "sbq-nominations").WithMessageBody(new CreateNominationMessage(Guid.NewGuid(), createNominationRequest.FileName));
        //await _serviceBusService.SendMessageAsync(builder, cancellationToken);

        await _messageSession.Send(new CreateNominationMessageNServiceBus(Guid.NewGuid(), createNominationRequest.FileName), sendOptions, cancellationToken);
    }
}