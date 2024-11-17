param(
  [parameter(Mandatory = $false)]
  [string]$tag = "latest",

  [parameter(Mandatory = $false)]
  [string]$repo = "docker.io",

  [parameter(Mandatory = $false)]
  [switch]$push
)

. $PSScriptRoot/../helpers.ps1
 
if ($repo) {
  $imageName = "$repo/ccr-attestation:$tag"
}
else {
  $imageName = "ccr-attestation:$tag"
}

$root = git rev-parse --show-toplevel
$external = Join-Path $root -ChildPath "/external"
git submodule update --init --recursive $external/confidential-sidecar-containers

docker build -t $imageName -f $external/confidential-sidecar-containers/docker/attestation-container/Dockerfile $external/confidential-sidecar-containers
CheckLastExitCode
if ($push) {
  docker push $imageName
  CheckLastExitCode
}