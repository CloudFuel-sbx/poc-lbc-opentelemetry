using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace LBC.OpenTelemetry.POC.Api.ServiceBus;

public sealed class ServiceBusMessageBuilder
{
    private static JsonSerializer _serializer = JsonSerializer.CreateDefault();

    public string QueueOrTopicName { get; init; }

    public string Subject { get; init; }

    public Uri Source { get; init; }

    private IServiceBusMessage MessageBody { get; set; }

    public ServiceBusMessageBuilder(string subject, string queueOrTopicName, [CallerFilePath] string source = null)
    {
        var caller = source.Substring(source.IndexOf("LBC."), source.Length - source.IndexOf("LBC.") - 3).Replace('\\', '.');

        Source = new Uri(caller, UriKind.Relative);
        Subject = subject;
        QueueOrTopicName = queueOrTopicName;
    }

    public ServiceBusMessageBuilder(string subscriptionId, string queueOrTopicName, IServiceBusMessage messageBody, [CallerFilePath] string source = null)
        : this(subscriptionId, queueOrTopicName, source)
    {
        MessageBody = messageBody;
    }

    public ServiceBusMessageBuilder WithMessageBody(IServiceBusMessage messageBody)
    {
        MessageBody = messageBody;
        return this;
    }

    public ServiceBusMessage Build()
    {
        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = string.Format("{0:D19}", DateTimeOffset.MaxValue.Ticks - DateTimeOffset.UtcNow.Ticks),
            Subject = Subject,
            Source = Source,
            Type = MessageBody.GetType(),
            DataContentType = "application/cloudevents+json",
            Time = DateTimeOffset.UtcNow,
            Data = MessageBody,
        };

        var messageBodyPayload = SerializeCloudEvent(cloudEvent);

        var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBodyPayload))
        {
            MessageId = Guid.NewGuid().ToString("D"),
            Subject = Subject,
            CorrelationId = Activity.Current.TraceId.ToString()
        };

        return serviceBusMessage;
    }

    private static string SerializeCloudEvent(CloudEvent cloudEvent)
    {
        var stream = new MemoryStream();
        var writer = new JsonTextWriter(new StreamWriter(stream));
        WriteCloudEvent(writer, cloudEvent);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCloudEvent(JsonWriter writer, CloudEvent cloudEvent)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(CloudEventsSpecVersion.SpecVersionAttribute.Name);
        writer.WriteValue(cloudEvent.SpecVersion.VersionId);
        var attributes = cloudEvent.GetPopulatedAttributes();
        foreach (var keyValuePair in attributes)
        {
            var attribute = keyValuePair.Key;
            var value = keyValuePair.Value;
            writer.WritePropertyName(attribute.Name);
            switch (CloudEventAttributeTypes.GetOrdinal(attribute.Type))
            {
                case CloudEventAttributeTypeOrdinal.Integer:
                    writer.WriteValue((int)value);
                    break;

                case CloudEventAttributeTypeOrdinal.Boolean:
                    writer.WriteValue((bool)value);
                    break;

                default:
                    writer.WriteValue(attribute.Type.Format(value));
                    break;
            }
        }

        if (cloudEvent.Data is object)
        {
            writer.WritePropertyName("Data");
            _serializer.Serialize(writer, cloudEvent.Data);
        }

        writer.WriteEndObject();
    }
}