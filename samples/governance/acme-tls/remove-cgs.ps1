[CmdletBinding()]
param
(
)

$root = git rev-parse --show-toplevel
$build = "$root/build"
$samples = "$root/samples"

docker compose -f $samples/governance/acme-tls/docker-compose.yml down
rm -rf $build/bin/governance

