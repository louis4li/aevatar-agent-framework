#!/bin/bash
# Shared Test Library for Agent Integration Tests

# Colors
export GREEN='\033[0;32m'
export RED='\033[0;31m'
export YELLOW='\033[1;33m'
export BLUE='\033[0;34m'
export NC='\033[0m' # No Color

# Common functions
check_mongodb() {
    echo "🔍 Checking MongoDB..."
    if nc -z localhost 27017 2>/dev/null; then
        echo "✅ MongoDB is running."
        return 0
    else
        echo "❌ MongoDB is not running."
        return 1
    fi
}

cleanup_processes() {
    echo "🧹 Cleaning up previous processes..."
    pkill -f "Aevatar.Silo" || true
    pkill -f "Aevatar.BusinessServer.HttpApi.Host" || true
    sleep 2
}

start_silo() {
    local env=${1:-Development}
    echo "📦 Starting Silo (${env})..."
    cd src/Aevatar.Silo
    ASPNETCORE_ENVIRONMENT=$env dotnet run > ../../logs/silo.log 2>&1 &
    SILO_PID=$!
    echo "   PID: $SILO_PID"
    cd ../..
    export SILO_PID
}

start_api() {
    local env=${1:-Development}
    echo "🌐 Starting HttpApi Host..."
    cd src/Aevatar.BusinessServer.HttpApi.Host
    ASPNETCORE_ENVIRONMENT=$env dotnet run > ../../logs/api.log 2>&1 &
    API_PID=$!
    echo "   PID: $API_PID"
    cd ../..
    export API_PID
}

wait_for_service() {
    local service_name=$1
    local wait_time=${2:-20}
    echo "⏳ Waiting ${wait_time}s for ${service_name} startup..."
    sleep $wait_time
}

check_health() {
    local response=$(curl -k -s -X GET https://localhost:44345/api/agent-demo/health)
    if echo "$response" | grep -q "Healthy"; then
        echo -e "   ${GREEN}✅ Health Check Passed${NC}"
        return 0
    else
        echo -e "   ${RED}❌ Health Check Failed${NC}"
        return 1
    fi
}

create_agent() {
    local response=$(curl -k -s -X POST https://localhost:44345/api/agent-demo/agents)
    if echo "$response" | grep -q "agentId"; then
        local agent_id=$(echo "$response" | grep -o '"agentId":"[^"]*"' | cut -d'"' -f4)
        echo "$agent_id"
        return 0
    else
        echo "ERROR"
        return 1
    fi
}

get_agent_stats() {
    local agent_id=$1
    curl -k -s -X GET "https://localhost:44345/api/agent-demo/agents/$agent_id/stats"
}

set_parent() {
    local child_id=$1
    local parent_id=$2
    curl -k -s -X POST "https://localhost:44345/api/agent-demo/agents/$child_id/parent/$parent_id"
}

publish_event() {
    local agent_id=$1
    local message=$2
    curl -k -s -X POST "https://localhost:44345/api/agent-demo/agents/$agent_id/events" \
        -H "Content-Type: application/json" \
        -d "{\"message\":\"$message\"}"
}

send_message() {
    local agent_id=$1
    local message=${2:-"Test Message"}
    publish_event "$agent_id" "$message"
}

show_logs() {
    local lines=${1:-30}
    echo ""
    echo "📋 Recent Silo Logs:"
    echo "===================="
    tail -$lines logs/silo.log | grep -E "(Agent|Event|Stream|Publish|Subscribe|ERROR|WARN)" || echo "No relevant logs"
    
    echo ""
    echo "📋 Recent API Logs:"
    echo "===================="
    tail -$lines logs/api.log | grep -E "(Agent|Event|Stream|Publish|Subscribe|ERROR|WARN)" || echo "No relevant logs"
}

stop_services() {
    echo "🛑 Stopping services..."
    [ ! -z "$SILO_PID" ] && kill $SILO_PID 2>/dev/null || true
    [ ! -z "$API_PID" ] && kill $API_PID 2>/dev/null || true
}

