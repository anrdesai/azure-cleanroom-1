[CmdletBinding()]
param
(
    [string]
    [Parameter(Mandatory)]
    $issuerUrl,

    [string] $port = "9290"
)

$root = git rev-parse --show-toplevel

. $root/build/helpers.ps1

Import-Module $PSScriptRoot/scripts/cgs.psm1 -Force -DisableNameChecking

Write-Output "Submitting enable oidc issuer proposal"
$proposalId = (propose-enable-oidc-issuer -port $port | jq -r '.proposalId')

Write-Output "Accepting the proposal"
Vote-Proposal -proposalId $proposalId -vote accept -port $port | jq

Write-Output "Generating oidc issuer signing key"
Generate-Oidc-Issuer-Signing-Key -port $port | jq

Write-Output "Submitting set oidc issuer URL proposal with value $issuerUrl"
$proposalId = (Propose-Set-Oidc-IssuerUrl -url $issuerUrl -port $port | jq -r '.proposalId')

Write-Output "Accepting the proposal"
Vote-Proposal -proposalId $proposalId -vote accept -port $port | jq

Write-Output "OIDC issuer configured. OIDC discovery document:"
$response = $(curl -sS -X GET $issuerUrl/.well-known/openid-configuration -k)
$response | jq

$jwks_uri = $response | jq -r '.jwks_uri'

Write-Output "JWKS document:"
curl -sS -X GET $jwks_uri -k | jq