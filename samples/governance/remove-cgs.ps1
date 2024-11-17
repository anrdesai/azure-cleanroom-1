[CmdletBinding()]
param
(
)

$root = git rev-parse --show-toplevel
$build = "$root/build"
$samples = "$root/samples"

docker compose -f $samples/governance/docker-compose.yml down

rm -rf $samples/governance/sandbox_common
rm -rf $build/bin/governance

