using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Agents;
using Aevatar.Agents.Abstractions.Attributes;
using Aevatar.Agents.Core;
using Business.Server;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Aevatar.BusinessServer.Application.Agents;

/// <summary>
/// Simple Business Agent demonstrating Agent Framework integration
/// Works with both Local and Orleans runtimes
/// </summary>
public class SimpleBusinessAgent : GAgentBase<SimpleBusinessAgentState>
{
    public SimpleBusinessAgent() : base()
    {
    }

    protected override async Task OnActivateAsync(CancellationToken ct = default)
    {
        await base.OnActivateAsync(ct);
        
        State.AgentId = Id.ToString();
        State.ProcessedEventsCount = 0;
        State.LastMessage = "Agent activated";
        State.LastUpdated = Timestamp.FromDateTime(DateTime.UtcNow);
        
        Logger.LogInformation("✅ SimpleBusinessAgent {Id} activated", Id);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            $"SimpleBusinessAgent {State.AgentId} " +
            $"(Processed: {State.ProcessedEventsCount} events, Last: {State.LastMessage})");
    }

    /// <summary>
    /// Process business message
    /// </summary>
    public async Task<string> ProcessMessageAsync(string message)
    {
        State.LastMessage = message;
        State.ProcessedEventsCount++;
        State.LastUpdated = Timestamp.FromDateTime(DateTime.UtcNow);

        Logger.LogInformation(
            "📩 Agent {Id} processed message: {Message} (Total: {Count})",
            Id, message, State.ProcessedEventsCount);

        // Publish event to stream
        var evt = new BusinessResponseEvent
        {
            ResponseId = Guid.NewGuid().ToString(),
            OriginalMessageId = Guid.NewGuid().ToString(),
            Response = $"Processed: {message}",
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        
        await PublishAsync(evt, EventDirection.Down);

        return $"✅ Processed by agent {State.AgentId}: {message}";
    }

    /// <summary>
    /// Get current agent statistics
    /// </summary>
    public Task<AgentStatistics> GetStatisticsAsync()
    {
        return Task.FromResult(new AgentStatistics
        {
            AgentId = State.AgentId ?? string.Empty,
            ProcessedCount = State.ProcessedEventsCount,
            LastMessage = State.LastMessage ?? string.Empty,
            LastUpdated = State.LastUpdated?.ToDateTime() ?? DateTime.UtcNow
        });
    }

    /// <summary>
    /// Event handler for BusinessMessageEvent
    /// </summary>
    [EventHandler]
    protected async Task HandleBusinessMessage(BusinessMessageEvent evt)
    {
        Logger.LogInformation("🎯 Received BusinessMessageEvent: {Content}", evt.Content);
        await ProcessMessageAsync(evt.Content);
    }
}

/// <summary>
/// Agent statistics DTO
/// </summary>
public class AgentStatistics
{
    public string AgentId { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
