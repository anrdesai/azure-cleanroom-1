param(
    [string]$telemetryFolder
)

$telemetryFolder = $($(Resolve-Path $telemetryFolder).Path)
$env:TELEMETRY_FOLDER = $telemetryFolder
docker build -f $PSScriptRoot/docker/Dockerfile.otelcollector-local -t otelcollector-local $PSScriptRoot
docker compose -p telemetryviewer-local -f $PSScriptRoot/docker/docker-compose.yaml up --wait