param(
  [parameter(Mandatory = $false)]
  [string]$tag = "latest",

  [parameter(Mandatory = $false)]
  [string]$repo = "docker.io",

  [parameter(Mandatory = $false)]
  [switch]$push
)

. $PSScriptRoot/helpers.ps1

$env:DOCKER_BUILDKIT = 1
$ErrorActionPreference = "Stop"

$services = @{
  "account-aggregator"         = @("0.0.0.0", "8000")
  "financial-information-user" = @("0.0.0.0", "8001")
  "business-rule-engine"       = @("0.0.0.0", "8080")
  "statement-analysis"         = @("0.0.0.0", "8080")
  "certificate-registry"       = @("0.0.0.0", "8080")
}

$root = git rev-parse --show-toplevel
$buildRoot = "$root/build"
foreach ($service in $services.Keys) {
  docker image build -t "$repo/$service-service:$tag" `
    -f $buildRoot/docker/samples/aa-flow-based-lending/Dockerfile.service `
    "$buildRoot/../samples/aa-flow-based-lending/src/$service" `
    --build-arg HOST=$($services[$service][0]) `
    --build-arg PORT=$($services[$service][1])
  CheckLastExitCode
}
