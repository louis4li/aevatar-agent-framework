#!/bin/bash

# Kafka Manager Script for Aevatar BusinessServer
# Manages Kafka, Zookeeper, and MongoDB using Docker Compose

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

show_help() {
    echo "Kafka Manager for Aevatar BusinessServer"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start       Start all services (Kafka, Zookeeper, MongoDB, Kafka-UI)"
    echo "  stop        Stop all services"
    echo "  restart     Restart all services"
    echo "  status      Show status of all services"
    echo "  logs        Show logs (follow mode)"
    echo "  clean       Stop and remove all containers and volumes"
    echo "  kafka-only  Start only Kafka and Zookeeper (no MongoDB)"
    echo "  check       Check if Kafka is ready for connections"
    echo ""
    echo "Examples:"
    echo "  $0 start                # Start all services"
    echo "  $0 status               # Check service status"
    echo "  $0 logs kafka           # Follow Kafka logs"
    echo "  $0 check                # Check Kafka connectivity"
}

check_docker() {
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}❌ Docker is not installed${NC}"
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        echo -e "${RED}❌ Docker daemon is not running${NC}"
        exit 1
    fi
}

start_services() {
    echo -e "${BLUE}🚀 Starting Aevatar Business Services...${NC}"
    check_docker
    
    docker-compose up -d
    
    echo ""
    echo -e "${GREEN}✅ Services started${NC}"
    echo ""
    echo "Waiting for services to be healthy..."
    
    for i in {1..30}; do
        if nc -z localhost 9092 2>/dev/null; then
            echo -e "${GREEN}✅ Kafka is ready (localhost:9092)${NC}"
            break
        fi
        echo -n "."
        sleep 1
    done
    
    echo ""
    echo -e "${GREEN}📊 Access Kafka UI: http://localhost:8082${NC}"
    echo -e "${GREEN}📊 MongoDB: mongodb://localhost:27017/AevatarBusiness${NC}"
}

stop_services() {
    echo -e "${YELLOW}🛑 Stopping Aevatar Business Services...${NC}"
    check_docker
    
    docker-compose stop
    echo -e "${GREEN}✅ Services stopped${NC}"
}

restart_services() {
    echo -e "${BLUE}🔄 Restarting Aevatar Business Services...${NC}"
    stop_services
    sleep 2
    start_services
}

show_status() {
    echo -e "${BLUE}📊 Service Status${NC}"
    echo ""
    check_docker
    
    docker-compose ps
    
    echo ""
    echo -e "${BLUE}Health Checks:${NC}"
    
    if nc -z localhost 9092 2>/dev/null; then
        echo -e "  Kafka (9092):    ${GREEN}✅ Running${NC}"
    else
        echo -e "  Kafka (9092):    ${RED}❌ Not accessible${NC}"
    fi
    
    if nc -z localhost 2181 2>/dev/null; then
        echo -e "  Zookeeper (2181): ${GREEN}✅ Running${NC}"
    else
        echo -e "  Zookeeper (2181): ${RED}❌ Not accessible${NC}"
    fi
    
    if nc -z localhost 27017 2>/dev/null; then
        echo -e "  MongoDB (27017):  ${GREEN}✅ Running${NC}"
    else
        echo -e "  MongoDB (27017):  ${RED}❌ Not accessible${NC}"
    fi
    
    if nc -z localhost 8082 2>/dev/null; then
        echo -e "  Kafka UI (8082):  ${GREEN}✅ Running${NC}"
        echo -e "  ${BLUE}→ http://localhost:8082${NC}"
    else
        echo -e "  Kafka UI (8082):  ${RED}❌ Not accessible${NC}"
    fi
}

show_logs() {
    echo -e "${BLUE}📋 Showing logs...${NC}"
    check_docker
    
    if [ -z "$1" ]; then
        docker-compose logs -f
    else
        docker-compose logs -f "$1"
    fi
}

clean_services() {
    echo -e "${RED}🧹 Cleaning up all services and volumes...${NC}"
    echo -e "${YELLOW}⚠️  This will remove all data!${NC}"
    read -p "Continue? (y/N) " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        check_docker
        docker-compose down -v
        echo -e "${GREEN}✅ Cleanup complete${NC}"
    else
        echo "Cancelled"
    fi
}

start_kafka_only() {
    echo -e "${BLUE}🚀 Starting Kafka and Zookeeper only...${NC}"
    check_docker
    
    docker-compose up -d zookeeper kafka kafka-ui
    
    echo ""
    echo "Waiting for Kafka to be ready..."
    for i in {1..30}; do
        if nc -z localhost 9092 2>/dev/null; then
            echo -e "${GREEN}✅ Kafka is ready (localhost:9092)${NC}"
            break
        fi
        echo -n "."
        sleep 1
    done
    
    echo ""
    echo -e "${GREEN}📊 Access Kafka UI: http://localhost:8082${NC}"
}

check_connectivity() {
    echo -e "${BLUE}🔍 Checking Kafka connectivity...${NC}"
    echo ""
    
    if nc -z localhost 9092 2>/dev/null; then
        echo -e "${GREEN}✅ Kafka is accessible at localhost:9092${NC}"
        
        # Try to list topics if kafka-topics is available
        if docker exec business-kafka kafka-topics --bootstrap-server localhost:9092 --list &> /dev/null; then
            echo ""
            echo -e "${BLUE}📝 Current topics:${NC}"
            docker exec business-kafka kafka-topics --bootstrap-server localhost:9092 --list
        fi
        
        return 0
    else
        echo -e "${RED}❌ Kafka is not accessible at localhost:9092${NC}"
        echo ""
        echo "Try:"
        echo "  $0 start      # Start all services"
        echo "  $0 status     # Check service status"
        return 1
    fi
}

# Main command dispatcher
case "${1:-}" in
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        restart_services
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs "$2"
        ;;
    clean)
        clean_services
        ;;
    kafka-only)
        start_kafka_only
        ;;
    check)
        check_connectivity
        ;;
    help|--help|-h)
        show_help
        ;;
    "")
        show_help
        ;;
    *)
        echo -e "${RED}❌ Unknown command: $1${NC}"
        echo ""
        show_help
        exit 1
        ;;
esac

