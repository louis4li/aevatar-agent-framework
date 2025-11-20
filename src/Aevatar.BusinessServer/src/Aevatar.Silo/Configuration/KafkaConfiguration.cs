using System.Collections.Generic;

namespace Aevatar.Silo.Configuration;

public class KafkaConfiguration
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "aevatar-silo-consumers";
    public Dictionary<string, KafkaTopicConfiguration> Topics { get; set; } = new();
}

public class KafkaTopicConfiguration
{
    public string Name { get; set; } = string.Empty;
    public int Partitions { get; set; } = 4;
    public int ReplicationFactor { get; set; } = 1;
    public bool AutoCreate { get; set; } = true;
}

public class StreamingOptions
{
    public string DefaultStreamNamespace { get; set; } = "agent-events";
    public string StreamProviderName { get; set; } = "KafkaStreamProvider";
    public bool AllowCustomNamespaces { get; set; } = true;
}

public class EventSourcingOptions
{
    public bool Enabled { get; set; } = true;
    public int SnapshotFrequency { get; set; } = 10;
    public string EventStoreType { get; set; } = "MongoDB";
}

