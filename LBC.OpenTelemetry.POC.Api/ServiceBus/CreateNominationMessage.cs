namespace LBC.OpenTelemetry.POC.Api.ServiceBus;

public record CreateNominationMessage(Guid Id, string MontovaFileName) : IServiceBusMessage
{
    public string GetType() => nameof(CreateNominationMessage);
}