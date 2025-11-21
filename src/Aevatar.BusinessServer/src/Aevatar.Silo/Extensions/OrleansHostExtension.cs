using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization; // Added
using Orleans.Streams;
/*
using Orleans.Streams.Kafka.Config;
using Orleans.Streams.Kafka.Core;
using MongoDB.Driver;
*/
using Serilog;

namespace Aevatar.Silo.Extensions;

/// <summary>
/// Orleans Host Configuration Extensions
/// Provides flexible configuration for Storage and Streaming providers
/// </summary>
public static class OrleansHostExtension
{
    /// <summary>
    /// Use Orleans with automatic configuration from appsettings.json
    /// Supports:
    /// - Storage: Memory (default) or MongoDB
    /// - Streaming: OrleansStream (default) or Kafka
    /// </summary>
    public static IHostBuilder UseOrleansConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            var configuration = context.Configuration;
            
            // Configure Orleans basics
            ConfigureOrleansCluster(siloBuilder, configuration);
            
            // Configure storage (Memory or MongoDB)
            ConfigureStorage(siloBuilder, configuration);
            
            // Configure streaming (Orleans Stream or Kafka)
            ConfigureStreaming(siloBuilder, configuration);
            
            // Configure additional Orleans options
            ConfigureOrleansOptions(siloBuilder, configuration);
            
            // Add Protobuf serializer
            siloBuilder.ConfigureServices(services => 
            {
                services.AddSerializer(serializerBuilder => 
                {
                    serializerBuilder.AddProtobufSerializer();
                });
            });
            
            Log.Information("✅ Orleans configuration completed");
        });
    }
    
    /// <summary>
    /// Configure Orleans cluster basics (ClusterId, ServiceId, Endpoints)
    /// </summary>
    private static void ConfigureOrleansCluster(ISiloBuilder siloBuilder, IConfiguration configuration)
    {
        var orleansConfig = configuration.GetSection("Orleans");
        var clusterId = orleansConfig.GetValue("ClusterId", "aevatar-cluster");
        var serviceId = orleansConfig.GetValue("ServiceId", "aevatar-service");
        var siloPort = orleansConfig.GetValue("SiloPort", 11111);
        var gatewayPort = orleansConfig.GetValue("GatewayPort", 30000);
        
        Log.Information("📋 Orleans Cluster Configuration:");
        Log.Information("  ClusterId: {ClusterId}", clusterId);
        Log.Information("  ServiceId: {ServiceId}", serviceId);
        Log.Information("  SiloPort: {SiloPort}", siloPort);
        Log.Information("  GatewayPort: {GatewayPort}", gatewayPort);
        
        siloBuilder
            .UseLocalhostClustering(siloPort, gatewayPort, null, serviceId, clusterId)
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = clusterId;
                options.ServiceId = serviceId;
            })
            .Configure<EndpointOptions>(options =>
            {
                options.AdvertisedIPAddress = System.Net.IPAddress.Loopback;
                options.SiloPort = siloPort;
                options.GatewayPort = gatewayPort;
            });
    }
    
    /// <summary>
    /// Configure storage providers (Memory or MongoDB)
    /// Default: Memory
    /// </summary>
    private static void ConfigureStorage(ISiloBuilder siloBuilder, IConfiguration configuration)
    {
        var storageConfig = configuration.GetSection("Storage");
        var storageProvider = storageConfig.GetValue("Provider", "Memory"); // Default: Memory
        
        Log.Information("🗄️  Configuring Storage:");
        Log.Information("  Provider: {Provider}", storageProvider);
        
        // Temporarily disabled MongoDB
        /*
        if (storageProvider.Equals("MongoDB", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureMongoDBStorage(siloBuilder, configuration);
        }
        else
        {
            ConfigureMemoryStorage(siloBuilder);
        }
        */
        ConfigureMemoryStorage(siloBuilder);
    }
    
    /// <summary>
    /// Configure Memory Storage (Default, fast for development)
    /// </summary>
    private static void ConfigureMemoryStorage(ISiloBuilder siloBuilder)
    {
        siloBuilder
            .AddMemoryGrainStorage("Default")
            .AddMemoryGrainStorage("PubSubStore")
            .AddMemoryGrainStorage("EventStoreStorage");
        
        Log.Information("  ✅ Using Memory Storage (fast, non-persistent)");
    }
    
    /*
    /// <summary>
    /// Configure MongoDB Storage (Production-ready, persistent)
    /// </summary>
    private static void ConfigureMongoDBStorage(ISiloBuilder siloBuilder, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB") 
            ?? "mongodb://localhost:27017/AevatarBusiness";
        var databaseName = configuration.GetSection("Storage")
            .GetValue("DatabaseName", "AevatarBusiness");
        
        Log.Information("  ConnectionString: {ConnectionString}", connectionString);
        Log.Information("  DatabaseName: {DatabaseName}", databaseName);
        
        // Memory storage for PubSub (required for streaming)
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
        
        // MongoDB storage for grain state and event sourcing
        // TODO: Fix MongoDBGrainStorageOptions API when package is updated
        Log.Warning("  ⚠️  MongoDB storage pending - using Memory as fallback");
        siloBuilder
            .AddMemoryGrainStorage("Default")
            .AddMemoryGrainStorage("EventStoreStorage");
        
        // Future implementation:
        // siloBuilder.AddMongoDBGrainStorage("Default", options => { ... })
        // siloBuilder.AddMongoDBGrainStorage("EventStoreStorage", options => { ... })
    }
    */
    
    /// <summary>
    /// Configure streaming providers (Orleans Stream or Kafka)
    /// Default: Orleans Stream (Memory-based, simple)
    /// </summary>
    private static void ConfigureStreaming(ISiloBuilder siloBuilder, IConfiguration configuration)
    {
        var streamConfig = configuration.GetSection("Streaming");
        var streamProvider = streamConfig.GetValue("Provider", "OrleansStream"); // Default: OrleansStream
        var streamNamespace = streamConfig.GetValue("DefaultNamespace", "agent-events");
        
        Log.Information("📡 Configuring Streaming:");
        Log.Information("  Provider: {Provider}", streamProvider);
        Log.Information("  DefaultNamespace: {Namespace}", streamNamespace);
        
        // Temporarily disabled Kafka
        /*
        if (streamProvider.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureKafkaStreaming(siloBuilder, configuration, streamNamespace);
        }
        else
        {
            ConfigureOrleansMemoryStreaming(siloBuilder, configuration, streamNamespace);
        }
        */
        ConfigureOrleansMemoryStreaming(siloBuilder, configuration, streamNamespace);
    }
    
    /// <summary>
    /// Configure Orleans Memory Streaming (Default, simple, no external dependencies)
    /// </summary>
    private static void ConfigureOrleansMemoryStreaming(ISiloBuilder siloBuilder, IConfiguration configuration, string streamNamespace)
    {
        var providerName = configuration.GetSection("Streaming").GetValue("ProviderName", "Default");
        
        siloBuilder.AddMemoryStreams(providerName, streamConfig =>
        {
            streamConfig.ConfigureStreamPubSub(StreamPubSubType.ExplicitGrainBasedAndImplicit);
            
            // Configure pulling agent options for better performance
            streamConfig.ConfigurePullingAgent(pullingAgentConfig => pullingAgentConfig.Configure(options =>
            {
                var streamSection = configuration.GetSection("Streaming");
                options.GetQueueMsgsTimerPeriod = TimeSpan.FromMilliseconds(
                    streamSection.GetValue("GetQueueMsgsTimerPeriodMs", 50));
            }));
        });
        
        Log.Information("  ✅ Using Orleans Memory Streaming (simple, no external dependencies)");
    }
    
    /*
    /// <summary>
    /// Configure Kafka Streaming (Production-ready, high-throughput)
    /// </summary>
    private static void ConfigureKafkaStreaming(ISiloBuilder siloBuilder, IConfiguration configuration, string streamNamespace)
    {
        var kafkaConfig = configuration.GetSection("Kafka");
        var bootstrapServers = kafkaConfig.GetValue("BootstrapServers", "localhost:9092");
        var consumerGroupId = kafkaConfig.GetValue("ConsumerGroupId", "aevatar-silo-consumers");
        var providerName = configuration.GetSection("Streaming").GetValue("ProviderName", "KafkaStreamProvider");
        
        Log.Information("  BootstrapServers: {BootstrapServers}", bootstrapServers);
        Log.Information("  ConsumerGroupId: {ConsumerGroupId}", consumerGroupId);
        
        siloBuilder.AddPersistentStreams(providerName, KafkaAdapterFactory.Create, streamBuilder =>
        {
            streamBuilder.ConfigureStreamPubSub(StreamPubSubType.ExplicitGrainBasedAndImplicit);
            
            streamBuilder.Configure<KafkaStreamOptions>(optionsBuilder => optionsBuilder.Configure(options =>
            {
                options.BrokerList = new List<string> { bootstrapServers };
                options.ConsumerGroupId = consumerGroupId;
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;
                options.PollTimeout = TimeSpan.FromMilliseconds(
                    kafkaConfig.GetValue("PollTimeoutMs", 100));
                
                // Add default topic
                options.AddTopic(streamNamespace, new TopicCreationConfig
                {
                    AutoCreate = true,
                    Partitions = kafkaConfig.GetValue("DefaultPartitions", 8),
                    ReplicationFactor = (short)kafkaConfig.GetValue("DefaultReplicationFactor", 1)
                });
                
                // Add additional topics from configuration
                var topics = kafkaConfig.GetSection("Topics");
                foreach (var topicSection in topics.GetChildren())
                {
                    var topicName = topicSection["Name"];
                    if (!string.IsNullOrEmpty(topicName) && topicName != streamNamespace)
                    {
                        options.AddTopic(topicName, new TopicCreationConfig
                        {
                            AutoCreate = topicSection.GetValue("AutoCreate", true),
                            Partitions = topicSection.GetValue("Partitions", 4),
                            ReplicationFactor = (short)topicSection.GetValue("ReplicationFactor", 1)
                        });
                        Log.Information("  📍 Added topic: {TopicName}", topicName);
                    }
                }
            }));
            
            // Configure pulling agent for stream processing
            streamBuilder.ConfigurePullingAgent(pullingAgentBuilder => pullingAgentBuilder.Configure(options =>
            {
                options.GetQueueMsgsTimerPeriod = TimeSpan.FromMilliseconds(
                    kafkaConfig.GetValue("GetQueueMsgsTimerPeriodMs", 50));
            }));
        });
        
        Log.Information("  ✅ Using Kafka Streaming (high-throughput, production-ready)");
    }
    */
    
    /// <summary>
    /// Configure additional Orleans options (timeouts, serialization, etc.)
    /// </summary>
    private static void ConfigureOrleansOptions(ISiloBuilder siloBuilder, IConfiguration configuration)
    {
        siloBuilder
            .Configure<SiloMessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromMinutes(5);
                options.SystemResponseTimeout = TimeSpan.FromMinutes(5);
            })
            .Configure<GrainCollectionOptions>(options =>
            {
                options.CollectionAge = TimeSpan.FromDays(30);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            });
    }
}

