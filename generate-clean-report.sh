#!/bin/bash
ROOT_DIR=$(pwd)
RESULTS_DIR="$ROOT_DIR/TestResults/Consolidated"

echo "Log: Limpiando resultados anteriores..."
rm -rf "$RESULTS_DIR"
mkdir -p "$RESULTS_DIR"

echo "Log: Buscando soluciones de servicios..."
SOLUTIONS=$(find "$ROOT_DIR/services" -maxdepth 2 -name "*.sln")

for SLN in $SOLUTIONS; do
    SERVICE_NAME=$(basename "$SLN" .sln)
    echo "--------------------------------------------------------"
    echo "Procesando Servicio: $SERVICE_NAME"
    echo "--------------------------------------------------------"
    
    dotnet test "$SLN" \
        --configuration Release \
        --settings "$ROOT_DIR/coverlet.runsettings" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$RESULTS_DIR/$SERVICE_NAME" \
        --no-build
done

echo "Script finalizado. Resultados de cobertura en $RESULTS_DIR"
