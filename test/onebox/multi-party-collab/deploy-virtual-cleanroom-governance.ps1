[CmdletBinding()]
param
(
    [switch]
    $NoBuild,

    [ValidateSet('mcr', 'local')]
    [string]$registry = "local",

    [Parameter(Mandatory)]
    [string]$projectName,

    [Parameter(Mandatory)]
    [string]$initialMemberName
)
$root = git rev-parse --show-toplevel

$outDir = "$root/test/onebox/multi-party-collab/generated/ccf"
mkdir -p $outDir
pwsh $root/samples/governance/azcli/deploy-cgs.ps1 `
    -initialMemberName $initialMemberName `
    -projectName $projectName `
    -ccfProjectName "ob-ccf" `
    -outDir $outDir `
    -NoTest `
    -NoBuild:$NoBuild `
    -registry $registry