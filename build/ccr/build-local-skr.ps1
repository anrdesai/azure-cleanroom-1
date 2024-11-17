param(
    [parameter(Mandatory = $false)]
    [string]$tag = "latest",
  
    [parameter(Mandatory = $false)]
    [string]$repo,
  
    [parameter(Mandatory = $false)]
    [switch]$push
)

$root = git rev-parse --show-toplevel
$buildRoot = "$root/build"
. $buildRoot/helpers.ps1

if ($repo) {
    $imageName = "$repo/local-skr:$tag"
}
else {
    $imageName = "local-skr:$tag"
}

docker image build -t $imageName -f $buildRoot/docker/Dockerfile.local-skr "$buildRoot/.."
CheckLastExitCode
if ($push) {
    docker push $imageName
    CheckLastExitCode
}