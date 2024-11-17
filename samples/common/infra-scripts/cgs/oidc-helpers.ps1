Import-Module $PSScriptRoot/../../../governance/scripts/cgs.psm1 -Force -DisableNameChecking
Import-Module $PSScriptRoot/../azure-helpers.psm1 -Force -DisableNameChecking

function Setup-OIDC-Provider {
    [CmdletBinding()]
    param (
        [string]$ccfEndpoint,
        [string]$resourceGroup,
        [string]$storageAccountName,
        [string]$containerName,
        [string]$clientPort
    )

    $outDir = "$($MyInvocation.PSScriptRoot)/sandbox_common"

    $oidcInfoJson = Get-Oidc-Issuer-Info -port $clientPort
    CheckLastExitCode

    $oidcInfo = $oidcInfoJson | ConvertFrom-Json

    # Skip setting the OIDC issuer URL for cases where already set.
    if ($null -ne $oidcInfo.tenantData) {
        Write-Host "OIDC information is already set"
        $issuerUrl = $oidcInfo.tenantData.issuerUrl

        Write-Host "Fetching openid-configuration.json"
        $result = curl -s "$issuerUrl/.well-known/openid-configuration"
        CheckLastExitCode

        $openIdConfig = $result | ConvertFrom-Json
        $jwksUri = $openIdConfig.jwks_uri

        Write-Host "Checking if JWKS document is accessible at $jwksUri"
        $result = curl -s "$jwksUri"
        CheckLastExitCode

        Write-Host "$result"
        return $issuerUrl
    }

    @"
{
    "issuer": "https://$storageAccountName.blob.core.windows.net/$containerName",
    "jwks_uri": "https://$storageAccountName.blob.core.windows.net/$containerName/openid/v1/jwks",
    "response_types_supported": [
    "id_token"
    ],
    "subject_types_supported": [
    "public"
    ],
    "id_token_signing_alg_values_supported": [
    "RS256"
    ]
}
"@ > "$outDir/openid-configuration.json"

    Write-Host "Uploading openid-configuration.json to storage account"
    $openIdConfigResult = az storage blob upload `
        --container-name "$containerName" `
        --file "$outDir/openid-configuration.json" `
        --name .well-known/openid-configuration `
        --account-name "$storageAccountName" `
        --overwrite `
        --auth-mode login
    CheckLastExitCode

    Write-Host "Verifying openid-configuration"
    $result = curl -s "https://$storageAccountName.blob.core.windows.net/$containerName/.well-known/openid-configuration"
    CheckLastExitCode

    curl -s -k $ccfEndpoint/app/oidc/keys | jq > $outDir/jwks.json
    CheckLastExitCode

    Write-Host "Uploading JWKS document"
    $result = az storage blob upload `
        --container-name "$containerName" `
        --file "$outDir/jwks.json" `
        --name openid/v1/jwks `
        --account-name "$storageAccountName" `
        --overwrite `
        --auth-mode login

    Write-Host "Verifying JWKS document"
    $result = curl -s "https://$storageAccountName.blob.core.windows.net/$containerName/openid/v1/jwks"
    CheckLastExitCode

    $issuerUrl = "https://$storageAccountName.blob.core.windows.net/$containerName"
    Set-Oidc-IssuerUrl -url $issuerUrl -port $clientPort
    return $issuerUrl
}

function Setup-OIDC-Infra {
    [CmdletBinding()]
    param (
        [string]$resourceGroup,
        [string]$storageAccountName,
        [string]$containerName
    )

    Write-Host "Creating OIDC storage account $storageAccountName in RG $resourceGroup"
    $storageAccountResult = (az storage account create `
            --resource-group "$resourceGroup" `
            --name "$storageAccountName" `
            --allow-blob-public-access true | ConvertFrom-Json)
    CheckLastExitCode

    Write-Host "Assigning 'Storage Blob Data Contributor' permissions to logged in user"
    $objectId = GetLoggedInEntityObjectId
    az role assignment create --role "Storage Blob Data Contributor" --scope $storageAccountResult.id --assignee-object-id $objectId --assignee-principal-type $(Get-Assignee-Principal-Type)
    CheckLastExitCode

    Write-Host "Waiting 15 seconds for permissions to take effect"
    Start-Sleep -Seconds 15

    Write-Host "Creating container $containerName in storage account $storageAccountName"
    $containerResult = az storage container create `
        --name "$containerName" `
        --account-name "$storageAccountName" `
        --public-access blob `
        --auth-mode login
    CheckLastExitCode
}

function Initialize-OIDC-Provider {
    [CmdletBinding()]
    param (
        [string]$clientPort
    )

    $oidcIssuerInfo = Get-Oidc-Issuer-Info -port $clientPort | ConvertFrom-Json

    if ($oidcIssuerInfo.enabled -eq "true") {
        Write-Host "OIDC issuer has already been enabled"
        return
    }

    Write-Output "Submitting enable oidc issuer proposal"
    $proposalId = $(propose-enable-oidc-issuer -port $clientPort | ConvertFrom-Json).proposalId

    Write-Output "Accepting the proposal"
    $proposal = (Vote-Proposal -proposalId $proposalId -vote accept -port $clientPort | ConvertFrom-Json)

    if ($proposal.proposalState -ne "Accepted") {
        Write-Host -ForegroundColor Red "Enabling OIDC issuer failed with proposal state `
            '$($proposal.proposalState)' after voting, proposalId: $($proposal.proposalId)"
        exit 1
    }

    Write-Output "Generating oidc issuer signing key"
    Generate-Oidc-Issuer-Signing-Key -port $clientPort
    CheckLastExitCode
}