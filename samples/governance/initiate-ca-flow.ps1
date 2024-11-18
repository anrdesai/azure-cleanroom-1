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

Write-Output "Submitting enable CA proposal"
$proposalId = (propose-enable-ca -contractId $contractId -port $port | jq -r '.proposalId')

Write-Output "Accepting the proposal"
Vote-Proposal -proposalId $proposalId -vote accept -port $port | jq

Write-Output "Generating CA signing key"
Generate-CA-Signing-Key -contract $contractId -port $port | jq

Write-Output "CA configured"
Get-CA-Info -contractId $contractId -port $port | jq

Write-Output "Submitting rotate CA signing key proposal"
$proposalId = (Propose-CA-Enable-Rotate-Signing-Key -contract $contractId -port $port | jq -r '.proposalId')

Write-Output "Accepting the proposal"
Vote-Proposal -proposalId $proposalId -vote accept -port $port | jq

Write-Output "Generating CA signing key"
Generate-CA-Signing-Key -contract $contractId -port $port | jq

Write-Output "CA configured"
Get-CA-Info -contractId $contractId -port $port | jq
