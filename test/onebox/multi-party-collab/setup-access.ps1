param(
    [Parameter(Mandatory = $true)]
    [string]$resourceGroup,
    [Parameter(Mandatory = $true)]
    [string]$governanceClient,
    [Parameter(Mandatory = $true)]
    [string]$contractId,
    [Parameter()]
    [ValidateSet("managed_identity", "service_principal")]
    [string]$identityType = "managed_identity",
    [string]$kvType,
    [string]$outDir = "",
    [switch]$usePreprovisionedOIDC
)

$ErrorActionPreference = 'Stop'

if ($outDir -eq "") {
    $outDir = "$PSScriptRoot/demo-resources/$resourceGroup"
}
else {
    $outDir = "$outDir/$resourceGroup"
}
. $outDir/names.generated.ps1

Import-Module $PSScriptRoot/common/infra-scripts/azure-helpers.psm1 -Force -DisableNameChecking

function Assign-Permission-KeyVault {
    param(
        [string]$role,
        [string]$objectId,
        [string]$principalType
    )

    Write-Host "Assigning permissions to key vault $KEYVAULT_NAME and resource group $resourceGroup"
    $keyVaultResult = (az keyvault show --name $KEYVAULT_NAME --resource-group $resourceGroup) | ConvertFrom-Json
    $roleAssignment = (az role assignment list `
            --assignee $objectId `
            --scope $keyVaultResult.id `
            --role $role) | ConvertFrom-Json

    if ($roleAssignment.Length -eq 1) {
        Write-Host "Key Vault Crypto Officer permission on the key vault already exists, skipping assignment"
    }
    else {
        Write-Host "Assigning Key Vault Crypto Officer on the Key Vault"
        az role assignment create `
            --role $role `
            --scope $keyVaultResult.id `
            --assignee-object-id $objectId `
            --assignee-principal-type $principalType
        CheckLastExitCode
    }
}

function Assign-Permission-HSM {
    param(
        [string]$role,
        [string]$objectId,
        [string]$principalType
    )

    $roleAssignment = (az keyvault role assignment list `
            --assignee-object-id $objectId `
            --hsm-name $MHSM_NAME `
            --role $role) | ConvertFrom-Json

    if ($roleAssignment.Length -eq 1) {
        Write-Host "$role permission on the HSM already exists, skipping assignment"
    }
    else {
        Write-Host "Assigning $role on the HSM"
        az keyvault role assignment create `
            --role $role `
            --scope "/" `
            --assignee-object-id $objectId `
            --hsm-name $MHSM_NAME `
            --assignee-principal-type ServicePrincipal
    }
}

function Assign-Secrets-User-Permission-KeyVault {
    param(
        [string]$objectId,
        [string]$principalType
    )

    $keyVaultResult = (az keyvault show `
            --name $KEYVAULT_NAME `
            --resource-group $resourceGroup) | ConvertFrom-Json
    az role assignment create `
        --role "Key Vault Secrets User" `
        --scope $keyVaultResult.id `
        --assignee-object-id $objectId `
        --assignee-principal-type ServicePrincipal
    CheckLastExitCode
}

function Assign-Permission-StorageAccount {
    param(
        [string]$objectId,
        [string]$principalType
    )

    $storageAccount = (az storage account show `
            --name $STORAGE_ACCOUNT_NAME `
            --resource-group $resourceGroup) | ConvertFrom-Json

    Write-Host "Assigning Storage Blob Data Contributor on the storage account"
    az role assignment create `
        --role "Storage Blob Data Contributor" `
        --scope $storageAccount.id `
        --assignee-object-id $objectId `
        --assignee-principal-type $principalType
    CheckLastExitCode
}

$isMhsm = $($kvType -eq "mhsm")
if ($identityType -eq "managed_identity") {
    $appId = (az identity show --name $MANAGED_IDENTITY_NAME --resource-group $resourceGroup | ConvertFrom-Json).principalId
}
else {
    $app = az ad sp list --display-name $ENTERPRISE_APP_NAME | ConvertFrom-Json
    $appId = $app.id
}

# Cleanroom needs both read/write permissions on storage account, hence assigning Storage Blob Data Contributor.
Assign-Permission-StorageAccount `
    -objectId $appId `
    -principalType ServicePrincipal

if ($isMhsm) {
    Write-Host "Assigning permissions on the HSM"
    Assign-Permission-HSM `
        -role "Managed HSM Crypto User" `
        -objectId $appId `
        -principalType ServicePrincipal
}
else {
    Write-Host "Assigning permissions on the Key Vault"
    Assign-Permission-KeyVault `
        -role "Key Vault Crypto Officer" `
        -objectId $appId `
        -principalType ServicePrincipal
}
Write-Host "Assigning Secrets User permission on the Key Vault"
Assign-Secrets-User-Permission-KeyVault `
    -objectId $appId `
    -principalType ServicePrincipal

# Set OIDC issuer.
$currentUser = (az account show) | ConvertFrom-Json
$tenantId = $currentUser.tenantid
$tenantData = (az cleanroom governance oidc-issuer show `
        --governance-client $governanceClient `
        --query "tenantData" | ConvertFrom-Json)
if ($null -ne $tenantData -and $tenantData.tenantId -eq $tenantId) {
    Write-Host -ForegroundColor Yellow "OIDC issuer already set for the tenant, skipping."
    $issuerUrl = $tenantData.issuerUrl
}
else {
    Write-Host "Setting up OIDC issuer for the tenant $tenantId"
    $storageAccountResult = $null
    if ($usePreprovisionedOIDC) {
        $storageAccountResult = (az storage account show `
                --name "cleanroomoidc") | ConvertFrom-Json
    }
    else {
        $storageAccountResult = (az storage account create `
                --resource-group "$resourceGroup" `
                --allow-shared-key-access false `
                --name "${OIDC_STORAGE_ACCOUNT_NAME}" `
                --allow-blob-public-access true) | ConvertFrom-Json
    }

    $objectId = GetLoggedInEntityObjectId
    Write-Host "Assigning 'Storage Blob Data Contributor' permissions to logged in user"
    az role assignment create `
        --role "Storage Blob Data Contributor" `
        --scope $storageAccountResult.id `
        --assignee-object-id $objectId `
        --assignee-principal-type $(Get-Assignee-Principal-Type)
    CheckLastExitCode

    if ($env:GITHUB_ACTIONS -eq "true") {
        $sleepTime = 90
        Write-Host "Waiting for $sleepTime seconds for permissions to get applied"
        Start-Sleep -Seconds $sleepTime
    }

    az storage container create `
        --name "${OIDC_CONTAINER_NAME}" `
        --account-name $storageAccountResult.name `
        --public-access blob `
        --auth-mode login
    CheckLastExitCode

    @"
{
"issuer": "https://$($storageAccountResult.name).blob.core.windows.net/${OIDC_CONTAINER_NAME}",
"jwks_uri": "https://$($storageAccountResult.name).blob.core.windows.net/${OIDC_CONTAINER_NAME}/openid/v1/jwks",
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
"@ > $outDir/openid-configuration.json

    az storage blob upload `
        --container-name "${OIDC_CONTAINER_NAME}" `
        --file $outDir/openid-configuration.json `
        --name .well-known/openid-configuration `
        --account-name $storageAccountResult.name `
        --overwrite `
        --auth-mode login
    CheckLastExitCode

    $ccfEndpoint = (az cleanroom governance client show --name $governanceClient | ConvertFrom-Json)
    $url = "$($ccfEndpoint.ccfEndpoint)/app/oidc/keys"
    curl -s -k $url | jq > $outDir/jwks.json

    az storage blob upload `
        --container-name "${OIDC_CONTAINER_NAME}" `
        --file $outDir/jwks.json `
        --name openid/v1/jwks `
        --account-name $storageAccountResult.name `
        --overwrite `
        --auth-mode login
    CheckLastExitCode

    az cleanroom governance oidc-issuer set-issuer-url `
        --governance-client $governanceClient `
        --url "https://$($storageAccountResult.name).blob.core.windows.net/${OIDC_CONTAINER_NAME}"
    $tenantData = (az cleanroom governance oidc-issuer show `
            --governance-client $governanceClient `
            --query "tenantData" | ConvertFrom-Json)
    $issuerUrl = $tenantData.issuerUrl
}

if ($env:COLLAB_FORCE_MANAGED_IDENTITY -eq "true") {
    Write-Host "Skipping setting up federation for $contractId due to MSFT tenant policy"
}
else {
    if ($identityType -eq "managed_identity") {
        Write-Host "Setting up federation on managed identity with issuerUrl $issuerUrl and subject $contractId"
        az identity federated-credential create `
            --name "$contractId-federation" `
            --identity-name $MANAGED_IDENTITY_NAME `
            --resource-group $resourceGroup `
            --issuer $issuerUrl `
            --subject $contractId
    }
    else {
        $parameters = @{
            name      = "$contractId-federation"
            issuer    = $issuerUrl
            subject   = $contractId
            audiences = @("api://AzureADTokenExchange")
        }

        $jsonParameters = $parameters | ConvertTo-Json

        write-Host "Creating federated credential with parameters: $jsonParameters"
        az ad app federated-credential create `
            --id $app.appId `
            --parameters $jsonParameters
        CheckLastExitCode
    }
}

if ($env:GITHUB_ACTIONS -eq "true") {
    $sleepTime = 30
    # See Note at https://learn.microsoft.com/en-us/azure/aks/workload-identity-deploy-cluster#create-the-federated-identity-credential
    Write-Host "Waiting for $sleepTime seconds for federated identity credential to propagate after it is added"
    Start-Sleep -Seconds $sleepTime
}

function Get-Assignee-Principal-Type {
    if ($env:GITHUB_ACTIONS -eq "true") {
        return "ServicePrincipal"
    }
    else {
        return "User"
    }
}