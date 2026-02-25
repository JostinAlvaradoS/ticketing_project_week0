#!/bin/bash
set -e

echo "Waiting for Kafka to be ready..."
until kafka-broker-api-versions --bootstrap-server localhost:9092 > /dev/null 2>&1; do
    sleep 1
done

echo "Kafka is ready. Creating topics..."

kafka-topics --create --if-not-exists \
    --bootstrap-server localhost:9092 \
    --replication-factor 1 \
    --partitions 1 \
    --topic reservation-created

kafka-topics --create --if-not-exists \
    --bootstrap-server localhost:9092 \
    --replication-factor 1 \
    --partitions 1 \
    --topic reservation-expired

kafka-topics --create --if-not-exists \
    --bootstrap-server localhost:9092 \
    --replication-factor 1 \
    --partitions 1 \
    --topic payment-succeeded

kafka-topics --create --if-not-exists \
    --bootstrap-server localhost:9092 \
    --replication-factor 1 \
    --partitions 1 \
    --topic payment-failed

kafka-topics --create --if-not-exists \
    --bootstrap-server localhost:9092 \
    --replication-factor 1 \
    --partitions 1 \
    --topic ticket-issued

echo "Topics created successfully!"
kafka-topics --list --bootstrap-server localhost:9092
