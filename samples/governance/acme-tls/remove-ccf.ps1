[CmdletBinding()]
param
(
)

$root = git rev-parse --show-toplevel
$build = "$root/build"
$samples = "$root/samples"

rm -rf $samples/governance/acme-tls/sandbox_common
rm -rf $build/bin/governance

