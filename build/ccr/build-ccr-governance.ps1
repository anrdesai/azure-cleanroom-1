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
  $imageName = "$repo/ccr-governance:$tag"
}
else {
  $imageName = "ccr-governance:$tag"
}

$root = git rev-parse --show-toplevel
docker image build -t $imageName -f $PSScriptRoot/../docker/Dockerfile.ccr-governance $root
CheckLastExitCode

if ($push) {
  docker push $imageName
  CheckLastExitCode
}