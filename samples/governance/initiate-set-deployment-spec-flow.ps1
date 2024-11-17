[CmdletBinding()]
param
(
    [string]
    $contractId,

    [string] $port = "9290"
)

$root = git rev-parse --show-toplevel

. $root/build/helpers.ps1

Import-Module $PSScriptRoot/scripts/cgs.psm1 -Force -DisableNameChecking

Write-Output "Submitting deployment spec proposal under contract $contractId"
$spec = @"
{
    "armTemplate": "something"
}
"@
$proposalId = (Propose-DeploymentSpec -contractId $contractId -spec $spec -port $port | jq -r '.proposalId')

Write-Output "Accepting the deployment spec proposal"
Vote-Proposal -proposalId $proposalId -vote accept -port $port | jq

Write-Output "Deployment spec:"
$p = (Get-DeploymentSpec -contractId $contractId -port $port)
$p | jq