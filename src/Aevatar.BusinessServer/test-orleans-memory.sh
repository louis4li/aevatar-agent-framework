#!/bin/bash

# Test Orleans Runtime with Memory Storage

set -e

BASEURL="https://localhost:44345"
API_PATH="/api/agent-demo"
CURL_OPTS="-k"

echo "🧪 Orleans Runtime Test (Memory Storage)"
echo "========================================"
echo ""

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Test 1: Health Check
echo -e "${BLUE}📋 Test 1: Health Check${NC}"
HEALTH=$(curl -s ${CURL_OPTS} ${BASEURL}${API_PATH}/health)
echo "Response: $HEALTH"
if echo "$HEALTH" | grep -q "Healthy"; then
    echo -e "${GREEN}✅ Health check passed${NC}"
else
    echo -e "${RED}❌ Health check failed${NC}"
    exit 1
fi
echo ""

# Test 2: Create Agent
echo -e "${BLUE}🤖 Test 2: Create Agent${NC}"
CREATE_RESPONSE=$(curl -s ${CURL_OPTS} -X POST ${BASEURL}${API_PATH}/agents \
  -H "Content-Type: application/json")
echo "Response: $CREATE_RESPONSE"
AGENT_ID=$(echo $CREATE_RESPONSE | grep -o '"agentId":"[^"]*"' | cut -d'"' -f4)
echo -e "Agent ID: ${YELLOW}$AGENT_ID${NC}"
if [ -z "$AGENT_ID" ]; then
    echo -e "${RED}❌ Failed to create agent${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Agent created successfully${NC}"
echo ""

# Test 3: Send Message
echo -e "${BLUE}📨 Test 3: Send Message${NC}"
MESSAGE_RESPONSE=$(curl -s ${CURL_OPTS} -X POST ${BASEURL}${API_PATH}/agents/${AGENT_ID}/messages \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello from Orleans Memory!"}')
echo "Response: $MESSAGE_RESPONSE"
if echo "$MESSAGE_RESPONSE" | grep -q "Processed"; then
    echo -e "${GREEN}✅ Message processed${NC}"
else
    echo -e "${RED}❌ Message processing failed${NC}"
    exit 1
fi
echo ""

# Test 4: Get Statistics
echo -e "${BLUE}📊 Test 4: Get Statistics${NC}"
STATS_RESPONSE=$(curl -s ${CURL_OPTS} ${BASEURL}${API_PATH}/agents/${AGENT_ID}/stats)
echo "Response: $STATS_RESPONSE"
if echo "$STATS_RESPONSE" | grep -q "processedEventsCount"; then
    echo -e "${GREEN}✅ Statistics retrieved${NC}"
else
    echo -e "${RED}❌ Failed to get statistics${NC}"
    exit 1
fi
echo ""

# Test 5: Multiple Messages
echo -e "${BLUE}🔄 Test 5: Multiple Messages${NC}"
for i in {1..3}; do
    curl -s ${CURL_OPTS} -X POST ${BASEURL}${API_PATH}/agents/${AGENT_ID}/messages \
      -H "Content-Type: application/json" \
      -d "{\"message\": \"Orleans message $i\"}" > /dev/null
done
echo -e "${GREEN}✅ Multiple messages sent${NC}"
echo ""

# Test 6: Verify Final Stats
echo -e "${BLUE}📈 Test 6: Verify Final Statistics${NC}"
FINAL_STATS=$(curl -s ${CURL_OPTS} ${BASEURL}${API_PATH}/agents/${AGENT_ID}/stats)
echo "Final stats: $FINAL_STATS"
COUNT=$(echo $FINAL_STATS | grep -o '"processedEventsCount":[0-9]*' | grep -o '[0-9]*')
echo -e "Processed events: ${YELLOW}$COUNT${NC}"
if [ "$COUNT" -ge 4 ]; then
    echo -e "${GREEN}✅ Statistics correct (≥4 events)${NC}"
else
    echo -e "${RED}❌ Statistics incorrect${NC}"
    exit 1
fi
echo ""

echo -e "${GREEN}═══════════════════════════════════${NC}"
echo -e "${GREEN}✅ Orleans Memory Test Passed!${NC}"
echo -e "${GREEN}═══════════════════════════════════${NC}"
echo ""
echo -e "Agent ID: ${YELLOW}$AGENT_ID${NC}"
echo ""

