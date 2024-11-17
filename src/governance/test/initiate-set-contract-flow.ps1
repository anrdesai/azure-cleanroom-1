[CmdletBinding()]
param
(
    [string] $contractId = "1234"
)

$root = git rev-parse --show-toplevel
$port_member0 = "8290"
$port_member1 = "8291"
$port_member2 = "8292"

. $root/build/helpers.ps1

Import-Module $root/samples/governance/scripts/cgs.psm1 -Force -DisableNameChecking

function verifyContractState([string] $expectedState) {
    $contract = (Get-Contract -id $contractId -port $port_member0)
    $contract | jq
    $state = "$contract" | jq -r ".state"
    if ($state -cne $expectedState) {
        Write-Error "contract is in state: $state, expected state: $expectedState"
        exit 1
    }
}

Write-Output "Adding contract"
$data = '{"hello": "world"}'
Create-Contract -data $data -id $contractId -port $port_member0
verifyContractState("Draft")

Write-Output "Submitting contract proposal as member0"
$version = (Get-Contract -id $contractId -port $port_member0 | jq -r ".version")
$proposalId = (Propose-Contract -version $version -id $contractId -port $port_member0 | jq -r '.proposalId')
verifyContractState("Proposed")

Write-Output "Accepting the contract proposal as member0"
Vote-Contract -id $contractId -proposalId $proposalId -vote accept -port $port_member0 | jq

Write-Output "Accepting the contract proposal as member1"
Vote-Contract -id $contractId -proposalId $proposalId -vote accept -port $port_member1 | jq

Write-Output "Accepting the contract proposal as member2"
Vote-Contract -id $contractId -proposalId $proposalId -vote accept -port $port_member2 | jq

verifyContractState("Accepted")

pwsh $PSScriptRoot/initiate-set-cleanroom-policy-flow.ps1 -contractId $contractId
