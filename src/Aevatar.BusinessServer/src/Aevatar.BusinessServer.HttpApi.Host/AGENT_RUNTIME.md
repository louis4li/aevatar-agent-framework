# Agent Runtime Integration

This document describes how the Aevatar Agent Framework is integrated into the BusinessServer HttpApi.Host.

---

## 🎯 Overview

The HttpApi.Host supports **two agent runtime modes**:

1. **Local** (Default) - In-memory, fast, no external dependencies
2. **Orleans** - Distributed, persistent, scalable

---

## 🔧 Configuration

### Default: Local Runtime

By default, the system uses **Local Runtime** which requires no additional setup:

```json
{
  "AgentRuntime": {
    "RuntimeType": "Local"
  }
}
```

**Advantages:**
- ✅ Zero configuration
- ✅ Fast startup
- ✅ Perfect for development
- ✅ No external dependencies (Orleans Silo)

### Option: Orleans Runtime

To use **Orleans Runtime**, update `appsettings.json`:

```json
{
  "AgentRuntime": {
    "RuntimeType": "Orleans",
    "Orleans": {
      "ClusterId": "aevatar-cluster",
      "ServiceId": "aevatar-service",
      "SiloPort": 11111,
      "GatewayPort": 30000,
      "UseLocalhostClustering": true,
      "StreamProviderName": "DefaultStreamProvider"
    }
  }
}
```

**Requirements:**
- ✅ Orleans Silo must be running (see `/src/Aevatar.Silo`)
- ✅ Cluster configuration must match Silo

**Advantages:**
- ✅ Distributed agents across multiple nodes
- ✅ Persistent agent state
- ✅ Horizontal scalability
- ✅ Production-ready

---

## 🚀 Usage

### Starting with Local Runtime

```bash
cd src/Aevatar.BusinessServer/src/Aevatar.BusinessServer.HttpApi.Host
dotnet run
```

No additional setup needed! The API is ready to use agents immediately.

### Starting with Orleans Runtime

**Step 1: Start Orleans Silo**

```bash
cd src/Aevatar.Silo
dotnet run
```

Wait for the silo to be ready:
```
[16:43:39 INF] -------------- Started silo S127.0.0.1:11111:122633019 --------------
```

**Step 2: Update HttpApi.Host Configuration**

Edit `appsettings.json`:
```json
{
  "AgentRuntime": {
    "RuntimeType": "Orleans"
  }
}
```

**Step 3: Start HttpApi.Host**

```bash
cd src/Aevatar.BusinessServer.HttpApi.Host
dotnet run
```

---

## 📚 Agent Services

The following services are automatically registered based on runtime type:

### Common Services (All Runtimes)

- `IEventStore` - Event store for event sourcing
- `IEventDeduplicator` - Prevents duplicate event processing

### Local Runtime Services

- `IGAgentActorFactory` → `LocalGAgentActorFactory`
- `ISubscriptionManager` → `LocalSubscriptionManager`
- `LocalMessageStreamRegistry` - In-memory message streams

### Orleans Runtime Services

- `IGAgentActorFactory` → `OrleansGAgentActorFactory`
- `ISubscriptionManager` → `OrleansSubscriptionManager`
- `IClusterClient` - Orleans cluster client

---

## 🧪 Example: Using Agents in Controllers

```csharp
using Aevatar.Agents.Abstractions;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IGAgentActorFactory _actorFactory;

    public AgentController(IGAgentActorFactory actorFactory)
    {
        _actorFactory = actorFactory;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateAgent([FromBody] AgentRequest request)
    {
        // Works with both Local and Orleans runtimes!
        var agent = await _actorFactory.CreateGAgentActorAsync<MyAgent>(
            Guid.NewGuid());
        
        // Use the agent...
        await agent.DoSomethingAsync();
        
        return Ok();
    }
}
```

**The same code works for both Local and Orleans runtimes!** 🎉

---

## 🔄 Switching Runtimes

### Development → Production

**Development (Local):**
```json
{ "AgentRuntime": { "RuntimeType": "Local" } }
```

**Production (Orleans):**
```json
{ "AgentRuntime": { "RuntimeType": "Orleans" } }
```

No code changes needed! Just update the configuration.

---

## 📊 Runtime Comparison

| Feature | Local | Orleans |
|---------|-------|---------|
| **Setup Complexity** | ✅ Zero | ⚠️ Requires Silo |
| **Performance** | ⚡ Very Fast | 🚀 Fast |
| **Scalability** | ❌ Single Process | ✅ Horizontal |
| **State Persistence** | ❌ Memory Only | ✅ Persistent |
| **Production Ready** | ⚠️ Development | ✅ Yes |
| **External Dependencies** | ✅ None | ⚠️ Silo + Storage |

---

## 🐛 Troubleshooting

### Issue: Orleans runtime fails to connect

**Error:** `Unable to connect to Orleans cluster`

**Solution:**
1. Ensure Aevatar.Silo is running:
   ```bash
   cd src/Aevatar.Silo
   dotnet run
   ```
2. Verify cluster configuration matches between Silo and HttpApi.Host
3. Check firewall/network settings for ports 11111 and 30000

### Issue: Agents not found in Local runtime

**Error:** `Agent not registered`

**Solution:**
Local runtime creates agents on-demand. Ensure your agent is properly defined and the factory is called correctly.

---

## 📖 Related Documentation

- [Aevatar.Silo README](../Aevatar.Silo/README.md) - Orleans Silo configuration
- [Agent Framework Examples](../../../../examples/) - Usage examples
- [Orleans Documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)

---

**Built with Aevatar Agent Framework** 🌊  
**Flexible | Scalable | Production-Ready** 🚀

