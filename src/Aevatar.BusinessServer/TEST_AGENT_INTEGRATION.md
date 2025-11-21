# Agent Integration Test Guide

## 📊 Test Status Summary

| Test | Status | Notes |
|------|--------|-------|
| 1. Local Runtime | ✅ **PASSED** | All 6 tests passed, ready for use |
| 2. Orleans Memory | ⚠️ **BLOCKED** | Version compatibility issue (see Troubleshooting) |
| 3. Orleans MongoDB | ⚠️ **PENDING** | Requires Test 2 resolution |
| 4. Orleans Stream | ⚠️ **PENDING** | Requires Test 2 resolution |
| 5. Kafka Stream | ⚠️ **PENDING** | Requires Test 2 resolution + Kafka setup |

**📋 Detailed Results:** See `AGENT_INTEGRATION_TEST_RESULTS.md`

---

## 🎯 Overview

This guide provides step-by-step instructions to test the Aevatar Agent Framework integration in BusinessServer.

**Test Progression:**
1. ✅ Local Runtime (in-memory, fast) - **COMPLETE**
2. ⏭️ Orleans with Memory Storage - **BLOCKED**
3. ⏭️ Orleans with MongoDB Storage - **PENDING**
4. ⏭️ Orleans with Memory Stream - **PENDING**
5. ⏭️ Orleans with Kafka Stream - **PENDING**

---

## 📋 Prerequisites

- .NET 10.0 SDK
- MongoDB (for Orleans + MongoDB tests)
- Apache Kafka (for Kafka stream tests)
- curl or Postman for API testing

---

## 🧪 Test 1: Local Runtime

**Description:** Test agent with in-memory Local runtime (no external dependencies).

### Step 1: Configure

Ensure `appsettings.json` has:

```json
{
  "AgentRuntime": {
    "RuntimeType": "Local"
  }
}
```

### Step 2: Start Server

```bash
cd src/Aevatar.BusinessServer/src/Aevatar.BusinessServer.HttpApi.Host
dotnet run
```

**Expected Output:**
```
[INFO] 🤖 Configuring Agent Runtime:
[INFO]   RuntimeType: Local
[INFO]   ✅ Using Local Runtime (in-memory, fast)
```

### Step 3: Test Health

```bash
curl http://localhost:44345/api/agent-demo/health
```

**Expected:**
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-20T...",
  "message": "Agent Framework is ready"
}
```

### Step 4: Create Agent

```bash
curl -X POST http://localhost:44345/api/agent-demo/agents \
  -H "Content-Type: application/json"
```

**Expected:**
```json
{
  "agentId": "f3a82b1d-...",
  "description": "SimpleBusinessAgent ... (Processed: 0 events, Last: Agent activated)",
  "createdAt": "2025-11-20T..."
}
```

**Save the agentId for next steps!**

### Step 5: Send Message

```bash
export AGENT_ID="<your-agent-id>"

curl -X POST http://localhost:44345/api/agent-demo/agents/$AGENT_ID/messages \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello from Local Runtime!"}'
```

**Expected:**
```json
{
  "agentId": "f3a82b1d-...",
  "response": "✅ Processed by agent ...: Hello from Local Runtime!",
  "processedAt": "2025-11-20T..."
}
```

### Step 6: Get Statistics

```bash
curl http://localhost:44345/api/agent-demo/agents/$AGENT_ID/stats
```

**Expected:**
```json
{
  "agentId": "f3a82b1d-...",
  "processedEventsCount": 1,
  "lastMessage": "Hello from Local Runtime!",
  "lastUpdated": "2025-11-20T..."
}
```

### ✅ Test 1 Complete!

---

## 🧪 Test 2: Orleans with Memory Storage

**Description:** Test agent with Orleans runtime using memory storage.

### Step 1: Start Orleans Silo

```bash
cd src/Aevatar.Silo
dotnet run
```

**Wait for:**
```
[INFO] -------------- Started silo S127.0.0.1:11111:... --------------
```

### Step 2: Configure HttpApi.Host

Update `appsettings.json`:

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

### Step 3: Start HttpApi.Host

```bash
cd src/Aevatar.BusinessServer/src/Aevatar.BusinessServer.HttpApi.Host
dotnet run
```

**Expected:**
```
[INFO] 🤖 Configuring Agent Runtime:
[INFO]   RuntimeType: Orleans
[INFO]   ✅ Using Orleans Runtime (distributed, scalable)
[INFO]     ClusterId: aevatar-cluster
[INFO]     ServiceId: aevatar-service
```

### Step 4: Repeat Tests from Test 1

Run all curl commands from Test 1 (health, create, send message, get stats).

**Note:** Agents persist in Orleans memory!

### ✅ Test 2 Complete!

---

## 🧪 Test 3: Orleans with MongoDB Storage

**Description:** Test agent persistence with MongoDB.

### Step 1: Start MongoDB

```bash
docker run -d -p 27017:27017 --name mongo mongo:latest
```

### Step 2: Configure Silo

Update `src/Aevatar.Silo/appsettings.Production.json`:

```json
{
  "Storage": {
    "Provider": "MongoDB"
  },
  "ConnectionStrings": {
    "Default": "mongodb://localhost:27017/AevatarAgents"
  }
}
```

### Step 3: Start Silo with Production Config

```bash
cd src/Aevatar.Silo
dotnet run --environment Production
```

### Step 4: Test Agent Persistence

1. Create an agent
2. Send messages
3. **Stop HttpApi.Host**
4. **Restart HttpApi.Host**
5. Query the same agent ID - state should persist!

```bash
curl http://localhost:44345/api/agent-demo/agents/$AGENT_ID/stats
```

**Expected:** Same stats as before restart!

### ✅ Test 3 Complete!

---

## 🧪 Test 4: Orleans with Memory Stream

**Description:** Test Orleans built-in memory streams.

### Step 1: Configure Silo

Ensure `appsettings.json` (or Production):

```json
{
  "Streaming": {
    "Provider": "OrleansStream"
  }
}
```

### Step 2: Test Event Streaming

Create parent-child agents and test event propagation:

```bash
# Create parent agent
PARENT_ID=$(curl -X POST http://localhost:44345/api/agent-demo/agents | jq -r '.agentId')

# Create child agent  
CHILD_ID=$(curl -X POST http://localhost:44345/api/agent-demo/agents | jq -r '.agentId')

# Set up hierarchy (future API)
# curl -X POST http://localhost:44345/api/agent-demo/agents/$CHILD_ID/parent/$PARENT_ID

# Send messages and observe event flow
curl -X POST http://localhost:44345/api/agent-demo/agents/$PARENT_ID/messages \
  -H "Content-Type: application/json" \
  -d '{"message": "Parent message"}'
```

### ✅ Test 4 Complete!

---

## 🧪 Test 5: Orleans with Kafka Stream

**Description:** Test Kafka-based event streaming.

### Step 1: Start Kafka

```bash
docker-compose up -d kafka zookeeper
```

### Step 2: Configure Silo

Update `appsettings.Production.json`:

```json
{
  "Streaming": {
    "Provider": "Kafka"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ConsumerGroupId": "aevatar-agents",
    "Topics": {
      "agent-events": {
        "Partitions": 3,
        "ReplicationFactor": 1
      }
    }
  }
}
```

### Step 3: Start Silo with Kafka

```bash
cd src/Aevatar.Silo
dotnet run --environment Production
```

### Step 4: Monitor Kafka Topics

```bash
# List topics
docker exec -it kafka kafka-topics --list --bootstrap-server localhost:9092

# Watch messages
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic agent-events \
  --from-beginning
```

### Step 5: Test Event Flow

Send messages to agents and observe Kafka topic activity:

```bash
curl -X POST http://localhost:44345/api/agent-demo/agents/$AGENT_ID/messages \
  -H "Content-Type: application/json" \
  -d '{"message": "Test Kafka streaming!"}'
```

**Check Kafka consumer output for events!**

### ✅ Test 5 Complete!

---

## 📊 Test Summary

| Test | Runtime | Storage | Streaming | Status |
|------|---------|---------|-----------|--------|
| 1 | Local | Memory | Local | ✅ |
| 2 | Orleans | Memory | Orleans | ⏭️ |
| 3 | Orleans | MongoDB | Orleans | ⏭️ |
| 4 | Orleans | MongoDB | Orleans Stream | ⏭️ |
| 5 | Orleans | MongoDB | Kafka | ⏭️ |

---

## 🐛 Troubleshooting

### Port Already in Use

```bash
lsof -i :44345
kill -9 <PID>
```

### Orleans Connection Errors

- Ensure Silo is running
- Check cluster configuration matches
- Verify ports 11111 and 30000 are open

### Orleans Version Compatibility Issue ⚠️

**Error:** `MissingMethodException: Method not found: 'Void Orleans.Runtime.Catalog.RegisterSystemTarget(Orleans.ISystemTarget)'`

**Cause:** Version mismatch between Orleans Core (9.2.1) and provider packages (8.x)

**Current Status:** Tests 2-5 are blocked until version compatibility is resolved.

**Solutions:**
1. Upgrade provider packages to 9.x (if available)
2. Downgrade Orleans Core to 8.x
3. Use conditional package references

**Workaround:** Use Local Runtime (Test 1) which is fully functional ✅

### MongoDB Connection Errors

```bash
docker logs mongo
docker restart mongo
```

### Kafka Connection Errors

```bash
docker-compose logs kafka
docker-compose restart kafka
```

## ⚠️ Known Issues

### Orleans Version Compatibility

**Issue:** Orleans 9.2.1 is incompatible with Orleans.Streams.Kafka 8.0.2 and Orleans.Providers.MongoDB 8.2.0

**Impact:** Tests 2-5 (Orleans-based tests) are currently blocked

**Status:** Documented in `AGENT_INTEGRATION_TEST_RESULTS.md`

**Workaround:** Use Local Runtime for development (fully tested and working)

---

## 📝 Notes

- **Local Runtime**: Perfect for development, no setup needed
- **Orleans Memory**: Adds distribution but state lost on restart
- **Orleans MongoDB**: Full persistence, production-ready
- **Kafka**: High-throughput event streaming for large-scale systems

---

**Built with Aevatar Agent Framework** 🌊  
**Flexible | Scalable | Production-Ready** 🚀

