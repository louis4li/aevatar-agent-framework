#!/bin/bash

# Kafka Stream Test Script
# Tests Orleans with Kafka as the streaming provider

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPT_DIR}/test-lib.sh"

echo "═══════════════════════════════════════════════════════════"
echo "   Kafka Stream Test for Aevatar Agent Framework"
echo "═══════════════════════════════════════════════════════════"
echo ""

# Check if Kafka is running
echo "🔍 Checking Kafka availability..."
if ! nc -z localhost 9092 2>/dev/null; then
    echo "❌ Kafka is not running on localhost:9092"
    echo ""
    echo "Please start Kafka first:"
    echo "  1. Install Kafka: brew install kafka"
    echo "  2. Start Zookeeper: brew services start zookeeper"
    echo "  3. Start Kafka: brew services start kafka"
    echo ""
    echo "Or use Docker:"
    echo "  docker-compose up -d kafka"
    echo ""
    exit 1
fi
echo "✅ Kafka is running"
echo ""

# Cleanup any previous test run
cleanup_services

# Modify appsettings.json for Kafka mode
echo "⚙️  Configuring for Kafka Stream mode..."
backup_and_modify_config "Kafka"

# Start MongoDB
echo "🍃 Starting MongoDB..."
start_mongodb

# Start Silo with Kafka
echo "🚀 Starting Silo with Kafka Stream Provider..."
ASPNETCORE_ENVIRONMENT=Development
start_silo

# Start API
echo "🌐 Starting HttpApi.Host..."
start_api

# Wait for services to be ready
echo "⏳ Waiting for services to initialize..."
sleep 10

# Run tests
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "   Running Kafka Stream Tests"
echo "═══════════════════════════════════════════════════════════"
echo ""

run_test "Create Parent Agent" \
    "POST" \
    "agents" \
    '{"name":"KafkaParent"}'

PARENT_ID=$(extract_agent_id "${LAST_RESPONSE}")
echo "   Parent ID: ${PARENT_ID}"
echo ""

run_test "Create Child Agent" \
    "POST" \
    "agents" \
    '{"name":"KafkaChild"}'

CHILD_ID=$(extract_agent_id "${LAST_RESPONSE}")
echo "   Child ID: ${CHILD_ID}"
echo ""

run_test "Set Parent-Child Relationship" \
    "POST" \
    "agents/${CHILD_ID}/parent/${PARENT_ID}" \
    ""

echo ""
sleep 2

run_test "Publish Event to Parent (should propagate to child via Kafka)" \
    "POST" \
    "agents/${PARENT_ID}/events" \
    '{"content":"Hello Kafka Stream!"}'

echo ""
sleep 3

run_test "Verify Child Received Event via Kafka" \
    "GET" \
    "agents/${CHILD_ID}/stats" \
    ""

if echo "${LAST_RESPONSE}" | grep -q '"processedEvents":1'; then
    echo "   ${GREEN}✅ Kafka Stream: PASSED${NC}"
    KAFKA_TEST_RESULT="PASSED"
else
    echo "   ${RED}❌ Kafka Stream: FAILED${NC}"
    echo "   Expected processedEvents >= 1, but got:"
    echo "${LAST_RESPONSE}" | jq '.processedEvents'
    KAFKA_TEST_RESULT="FAILED"
fi

# Display results
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "📊 Test Results Summary"
echo "═══════════════════════════════════════════════════════════"

if [ "${KAFKA_TEST_RESULT}" = "PASSED" ]; then
    echo "   ${GREEN}✅ Kafka Stream: PASSED${NC}"
else
    echo "   ${RED}❌ Kafka Stream: FAILED${NC}"
fi

# Show recent logs
show_recent_logs

echo ""
echo "✨ Kafka Stream Test Completed"
echo ""

# Cleanup
cleanup_services
restore_config

echo "🛑 All services stopped"

