using Aevatar.Silo.Messages;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.Agents;

/// <summary>
/// Kafka Producer Agent - Publishes messages to Kafka through Orleans Stream
/// Demonstrates: Orleans Stream integration with Kafka
/// </summary>
public class KafkaProducerAgent // : GAgentBase<KafkaProducerState>
{
    private readonly ILogger<KafkaProducerAgent> _logger;
    private readonly string _topic;

    public KafkaProducerAgent(ILogger<KafkaProducerAgent> logger, string topic = "agent-events")
    {
        _logger = logger;
        _topic = topic;
    }

    // TODO: Implement with GAgentBase when framework is integrated
    /*
    public async Task PublishMessageAsync(string content, Dictionary<string, string>? metadata = null)
    {
        var kafkaMessage = new KafkaMessageEvent
        {
            MessageId = Guid.NewGuid().ToString(),
            Topic = _topic,
            Content = content,
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            SenderId = AgentId.ToString()
        };

        if (metadata != null)
        {
            foreach (var (key, value) in metadata)
            {
                kafkaMessage.Metadata[key] = value;
            }
        }

        // Publish to Orleans Stream → Kafka
        await PublishAsync(kafkaMessage);

        // Update state
        State.MessagesPublished++;
        State.TotalBytesSent += EstimateMessageSize(kafkaMessage);
        State.LastPublishTime = Timestamp.FromDateTime(DateTime.UtcNow);
        State.TopicName = _topic;

        _logger.LogInformation(
            "[KafkaProducer] Published message {MessageId} to topic {Topic}, total: {Total}",
            kafkaMessage.MessageId,
            _topic,
            State.MessagesPublished
        );
    }

    public async Task<KafkaProducerState> GetStateAsync()
    {
        return State;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult($"Kafka Producer Agent for topic: {_topic}");
    }

    private long EstimateMessageSize(KafkaMessageEvent message)
    {
        return message.Content.Length + 
               message.Topic.Length + 
               message.MessageId.Length +
               message.Metadata.Sum(kv => kv.Key.Length + kv.Value.Length);
    }
    */
}

/// <summary>
/// Kafka Consumer Agent - Consumes messages from Kafka through Orleans Stream
/// Demonstrates: Event-driven message processing
/// </summary>
public class KafkaConsumerAgent // : GAgentBase<KafkaConsumerState>
{
    private readonly ILogger<KafkaConsumerAgent> _logger;

    public KafkaConsumerAgent(ILogger<KafkaConsumerAgent> logger)
    {
        _logger = logger;
    }

    // TODO: Implement with GAgentBase when framework is integrated
    /*
    [EventHandler(Priority = 1)]
    public async Task HandleKafkaMessage(KafkaMessageEvent message)
    {
        _logger.LogInformation(
            "[KafkaConsumer] Received message {MessageId} from {SenderId} on topic {Topic}",
            message.MessageId,
            message.SenderId,
            message.Topic
        );

        // Process message content
        await ProcessMessageAsync(message);

        // Update state
        State.MessagesConsumed++;
        State.TotalBytesReceived += EstimateMessageSize(message);
        State.LastConsumeTime = Timestamp.FromDateTime(DateTime.UtcNow);
        State.SubscriptionStatus = "active";
    }

    private async Task ProcessMessageAsync(KafkaMessageEvent message)
    {
        // Business logic here
        _logger.LogDebug("Processing message content: {Content}", message.Content);
        await Task.CompletedTask;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Kafka Consumer Agent");
    }

    private long EstimateMessageSize(KafkaMessageEvent message)
    {
        return message.Content.Length + 
               message.Topic.Length + 
               message.MessageId.Length +
               message.Metadata.Sum(kv => kv.Key.Length + kv.Value.Length);
    }
    */
}

