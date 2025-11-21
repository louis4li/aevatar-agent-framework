#!/bin/bash
pkill -f "Aevatar.Silo"
pkill -f "Aevatar.BusinessServer.HttpApi.Host"
rm -f /tmp/*.log
echo "🧹 Cleaned up processes and logs."

