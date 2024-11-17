[CmdletBinding()]
param
(
    [string]
    $outDir = "$PSScriptRoot/generated",

    [string]
    $ccfEndpoint = "https://host.docker.internal:9081",

    [string]
    $ccfOutDir = "",

    [string]
    $datastoreOutdir = "",

    [string]
    $contractId = "collab1-db",

    [switch]
    $y,

    [ValidateSet('mcr', 'local')]
    [string]$registry = "local"
)

#https://learn.microsoft.com/en-us/powershell/scripting/learn/experimental-features?view=powershell-7.4#psnativecommanderroractionpreference
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

# This script assumes a CCF instance was deployed in docker with the initial member that acts as the
# consumer for the multi-party collab sample.
$root = git rev-parse --show-toplevel
$collabSamplePath = "$root/test/onebox/multi-party-collab"

if ($ccfOutDir -eq "") {
    $ccfOutDir = "$root/test/onebox/multi-party-collab/generated/ccf"
}

if ($datastoreOutdir -eq "") {
    $datastoreOutdir = "$root/test/onebox/multi-party-collab/generated/datastores"
}

$serviceCert = $ccfOutDir + "/service_cert.pem"
if (-not (Test-Path -Path $serviceCert)) {
    throw "serviceCert at $serviceCert does not exist."
}

if ($env:GITHUB_ACTIONS -eq "true" -and $ccfEndpoint -eq "https://host.docker.internal:9081") {
    # 172.17.0.1: https://stackoverflow.com/questions/48546124/what-is-the-linux-equivalent-of-host-docker-internal
    $ccfEndpoint = "https://172.17.0.1:9081"
}

mkdir -p "$outDir/configurations"
$publisherConfig = "$outDir/configurations/publisher-config"
$consumerConfig = "$outDir/configurations/consumer-config"
$publisherDatastoreConfig = "$datastoreOutdir/db-access-publisher-datastore-config"
$consumerDatastoreConfig = "$datastoreOutdir/db-access-consumer-datastore-config"

mkdir -p "$datastoreOutdir/secrets"
$publisherSecretStoreConfig = "$datastoreOutdir/secrets/db-access-publisher-secretstore-config"
$consumerSecretStoreConfig = "$datastoreOutdir/secrets/db-access-consumer-secretstore-config"

$publisherLocalSecretStore = "$datastoreOutdir/secrets/db-access-publisher-secretstore-local"
$consumerLocalSecretStore = "$datastoreOutdir/secrets/db-access-consumer-secretstore-local"

$resourceGroupTags = ""
if ($env:GITHUB_ACTIONS -eq "true") {
    $publisherResourceGroup = "cl-ob-publisher-${env:JOB_ID}-${env:RUN_ID}"
    $consumerResourceGroup = "cl-ob-consumer-${env:JOB_ID}-${env:RUN_ID}"
    $resourceGroupTags = "github_actions=multi-party-collab-${env:JOB_ID}-${env:RUN_ID}"
}
else {
    $publisherResourceGroup = "cl-ob-publisher-${env:USER}"
    $consumerResourceGroup = "cl-ob-consumer-${env:USER}"
}

# Set tenant Id as a part of the consumer's member data.
# This is required to enable OIDC provider in the later steps.
$consumerTenantId = az account show --query "tenantId" --output tsv
$proposalId = (az cleanroom governance member set-tenant-id `
        --identifier consumer `
        --tenant-id $consumerTenantId `
        --query "proposalId" `
        --output tsv `
        --governance-client "ob-consumer-client")

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client "ob-consumer-client"

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# Publisher identity creation.
if (-not (Test-Path -Path "$ccfOutDir/publisher_cert.pem")) {
    az cleanroom governance member keygenerator-sh | bash -s -- --name "publisher" --out "$ccfOutDir"
}

# Invite publisher to the consortium.
$publisherTenantId = az account show --query "tenantId" --output tsv

# "consumer" member makes a proposal for adding the new member "publisher".
$proposalId = (az cleanroom governance member add `
        --certificate $ccfOutDir/publisher_cert.pem `
        --identifier "publisher" `
        --tenant-id $publisherTenantId `
        --query "proposalId" `
        --output tsv `
        --governance-client "ob-consumer-client")

# Vote on the above proposal to accept the membership.
az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client "ob-consumer-client"

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# "publisher" deploys client-side containers to interact with the governance service as the new member.
# Set overrides if local registry is to be used for CGS images.
if ($registry -eq "local") {
    $tag = cat "$root/test/onebox/multi-party-collab/generated/ccf/local-registry-tag.txt"
    $env:AZCLI_CGS_CLIENT_IMAGE = "localhost:5000/cgs-client:$tag"
    $env:AZCLI_CGS_UI_IMAGE = "localhost:5000/cgs-ui:$tag"
}

az cleanroom governance client deploy `
    --ccf-endpoint $ccfEndpoint `
    --signing-cert $ccfOutDir/publisher_cert.pem `
    --signing-key $ccfOutDir/publisher_privk.pem `
    --service-cert $ccfOutDir/service_cert.pem `
    --name "ob-publisher-client"

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# "publisher" accepts the invitation and becomes an active member in the consortium.
az cleanroom governance member activate --governance-client "ob-publisher-client"

# Create storage account, KV and MI resources.
pwsh $collabSamplePath/prepare-resources.ps1 `
    -resourceGroup $publisherResourceGroup `
    -resourceGroupTags $resourceGroupTags `
    -kvType akvpremium `
    -outDir $outDir
$pubResult = Get-Content "$outDir/$publisherResourceGroup/resources.generated.json" | ConvertFrom-Json

az cleanroom secretstore add `
    --name publisher-local-store `
    --config $publisherSecretStoreConfig `
    --backingstore-type Local_File `
    --backingstore-path $publisherLocalSecretStore

# Add DEK and KEK secret stores.
az cleanroom secretstore add `
    --name publisher-dek-store `
    --config $publisherSecretStoreConfig `
    --backingstore-type Azure_KeyVault `
    --backingstore-id $pubResult.dek.kv.id 

az cleanroom secretstore add `
    --name publisher-kek-store `
    --config $publisherSecretStoreConfig `
    --backingstore-type Azure_KeyVault_Managed_HSM `
    --backingstore-id $pubResult.kek.kv.id `
    --attestation-endpoint $pubResult.maa_endpoint

az cleanroom config init --cleanroom-config $publisherConfig

# if (!$y) {
#     Read-Host "prepare-resources done. Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# Creates the PostgreSQL instance with sample data.
. $collabSamplePath/db-access/deploy-postgresql.ps1
$db = Deploy-PostgreSQL -resourceGroup $publisherResourceGroup

# Open up the network access to the application container.
az cleanroom config disable-sandbox `
    --cleanroom-config $publisherConfig

$memberId = (az cleanroom governance client show `
        --name "ob-publisher-client" `
        --query "memberId" `
        --output tsv)
$DB_CONFIG_CGS_SECRET_NAME = "dbconfig"
$DB_CONFIG_CGS_SECRET_ID = "${memberId}:$DB_CONFIG_CGS_SECRET_NAME"
Write-Host "DB_CONFIG_CGS_SECRET_ID: $DB_CONFIG_CGS_SECRET_ID"

$identity = $(az resource show --ids $pubResult.mi.id --query "properties") | ConvertFrom-Json

# Create identity entry in the configuration.
az cleanroom config add-identity az-federated `
    --cleanroom-config $publisherConfig `
    -n publisher-identity `
    --client-id $identity.clientId `
    --tenant-id $identity.tenantId `
    --backing-identity cleanroom_cgs_oidc

$containerSuffix = $($($(New-Guid).Guid) -replace '-').ToLower()
Write-Host "Using container suffix {$containerSuffix} for application-telemetry"

# $pubResult below refers to the output of the prepare-resources.ps1 that was run earlier.
az cleanroom config set-logging `
    --cleanroom-config $publisherConfig `
    --storage-account $pubResult.sa.id `
    --identity publisher-identity `
    --datastore-config $publisherDatastoreConfig `
    --secretstore-config $publisherSecretStoreConfig `
    --datastore-secret-store publisher-local-store `
    --dek-secret-store publisher-dek-store `
    --kek-secret-store publisher-kek-store `
    --encryption-mode CPK `
    --container-suffix $containerSuffix

$containerSuffix = $($($(New-Guid).Guid) -replace '-').ToLower()
Write-Host "Using container suffix {$containerSuffix} for infrastructure-telemetry"

az cleanroom config set-telemetry `
    --cleanroom-config $publisherConfig `
    --storage-account $pubResult.sa.id `
    --identity publisher-identity `
    --datastore-config $publisherDatastoreConfig `
    --secretstore-config $publisherSecretStoreConfig `
    --datastore-secret-store publisher-local-store `
    --dek-secret-store publisher-dek-store `
    --kek-secret-store publisher-kek-store `
    --encryption-mode CPK `
    --container-suffix $containerSuffix

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# Create storage account, KV and MI resources.
pwsh $collabSamplePath/prepare-resources.ps1 `
    -resourceGroup $consumerResourceGroup `
    -resourceGroupTags $resourceGroupTags `
    -kvType akvpremium `
    -outDir $outDir
$conResult = Get-Content "$outDir/$consumerResourceGroup/resources.generated.json" | ConvertFrom-Json

az cleanroom secretstore add `
    --name consumer-local-store `
    --config $consumerSecretStoreConfig `
    --backingstore-type Local_File `
    --backingstore-path $consumerLocalSecretStore

$containerName = $($($(New-Guid).Guid) -replace '-').ToLower()
Write-Host "Using container name {$containerName} for datastore {consumer-db-output}"

# Create a datasource entry.
az cleanroom datastore add `
    --name consumer-db-output `
    --config $consumerDatastoreConfig `
    --secretstore consumer-local-store `
    --secretstore-config $consumerSecretStoreConfig `
    --encryption-mode CPK `
    --backingstore-type Azure_BlobStorage `
    --backingstore-id $conResult.sa.id `
    --container-name $containerName

az cleanroom config init --cleanroom-config $consumerConfig

# Add DEK and KEK secret stores.
az cleanroom secretstore add `
    --name consumer-dek-store `
    --config $consumerSecretStoreConfig `
    --backingstore-type Azure_KeyVault `
    --backingstore-id $conResult.dek.kv.id

az cleanroom secretstore add `
    --name consumer-kek-store `
    --config $consumerSecretStoreConfig `
    --backingstore-type Azure_KeyVault_Managed_HSM `
    --backingstore-id $conResult.kek.kv.id `
    --attestation-endpoint $conResult.maa_endpoint

$identity = $(az resource show --ids $conResult.mi.id --query "properties") | ConvertFrom-Json

# Create identity entry in the configuration.
az cleanroom config add-identity az-federated `
    --cleanroom-config $consumerConfig `
    -n consumer-identity `
    --client-id $identity.clientId `
    --tenant-id $identity.tenantId `
    --backing-identity cleanroom_cgs_oidc

# Create a datasink entry in the configuration.
az cleanroom config add-datasink `
    --cleanroom-config $consumerConfig `
    --datastore-name consumer-db-output `
    --datastore-config $consumerDatastoreConfig `
    --secretstore-config $consumerSecretStoreConfig `
    --dek-secret-store consumer-dek-store `
    --kek-secret-store consumer-kek-store `
    --identity consumer-identity

$sample_code = $(tar cz -C $collabSamplePath/db-access/consumer application | base64 -w 0)
az cleanroom config add-application `
    --cleanroom-config $consumerConfig `
    --name demo-app `
    --image "docker.io/golang@sha256:f43c6f049f04cbbaeb28f0aad3eea15274a7d0a7899a617d0037aec48d7ab010" `
    --command "bash -c 'echo `$CODE | base64 -d | tar xz; cd application; go get; go run main.go'" `
    --mounts "src=consumer-db-output,dst=/mnt/remote/output" `
    --env-vars OUTPUT_LOCATION=/mnt/remote/output `
    DB_CONFIG_CGS_SECRET_ID=$DB_CONFIG_CGS_SECRET_ID `
    CODE="$sample_code" `
    --cpu 0.5 `
    --memory 4

# Generate the cleanroom config which contains all the datasources, sinks and applications that are
# configured by both the producer and consumer.
az cleanroom config view `
    --cleanroom-config $consumerConfig `
    --configs $publisherConfig `
    --out-file $outDir/configurations/cleanroom-config

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

az cleanroom config validate --cleanroom-config $outDir/configurations/cleanroom-config

$data = Get-Content -Raw $outDir/configurations/cleanroom-config
az cleanroom governance contract create `
    --data "$data" `
    --id $contractId `
    --governance-client "ob-consumer-client"

# Submitting a contract proposal.
$version = (az cleanroom governance contract show `
        --id $contractId `
        --query "version" `
        --output tsv `
        --governance-client "ob-consumer-client")

az cleanroom governance contract propose `
    --version $version `
    --id $contractId `
    --governance-client "ob-consumer-client"

$contract = (az cleanroom governance contract show `
        --id $contractId `
        --governance-client "ob-publisher-client" | ConvertFrom-Json)

# Accept it.
az cleanroom governance contract vote `
    --id $contractId `
    --proposal-id $contract.proposalId `
    --action accept `
    --governance-client "ob-publisher-client"

$contract = (az cleanroom governance contract show `
        --id $contractId `
        --governance-client "ob-consumer-client" | ConvertFrom-Json)

# Accept it.
az cleanroom governance contract vote `
    --id $contractId `
    --proposal-id $contract.proposalId `
    --action accept `
    --governance-client "ob-consumer-client"

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

mkdir -p $outDir/deployments
# Set overrides if local registry is to be used for clean room container images.
if ($registry -eq "local") {
    $env:AZCLI_CLEANROOM_CONTAINER_REGISTRY_URL = "localhost:5001"
    $env:AZCLI_CLEANROOM_SIDECARS_VERSIONS_DOCUMENT_URL = "localhost:5001/sidecar-digests:latest"
}
az cleanroom governance deployment generate `
    --contract-id $contractId `
    --governance-client "ob-consumer-client" `
    --output-dir $outDir/deployments `
    --security-policy-creation-option allow-all

if ($env:COLLAB_FORCE_MANAGED_IDENTITY -eq "true") {
    Import-Module $root/test/onebox/multi-party-collab/force-managed-identity.ps1 -Force -DisableNameChecking
    Force-Managed-Identity `
        -deploymentTemplateFile "$outDir/deployments/cleanroom-arm-template.json" `
        -managedIdentities @($pubResult.mi.id, $conResult.mi.id)
}

az cleanroom governance deployment template propose `
    --template-file $outDir/deployments/cleanroom-arm-template.json `
    --contract-id $contractId `
    --governance-client "ob-consumer-client"

az cleanroom governance deployment policy propose `
    --policy-file $outDir/deployments/cleanroom-governance-policy.json `
    --contract-id $contractId `
    --governance-client "ob-consumer-client"

# Propose enabling log and telemetry collection during cleanroom execution.
az cleanroom governance contract runtime-option propose `
    --option logging `
    --action enable `
    --contract-id $contractId `
    --governance-client "ob-consumer-client"

az cleanroom governance contract runtime-option propose `
    --option telemetry `
    --action enable `
    --contract-id $contractId `
    --governance-client "ob-consumer-client"

$clientName = "ob-publisher-client"
pwsh $collabSamplePath/verify-deployment-proposals.ps1 `
    -cleanroomConfig $publisherConfig `
    -governanceClient $clientName

# Vote on the proposed deployment template.
$proposalId = az cleanroom governance deployment template show `
    --contract-id $contractId `
    --governance-client $clientName `
    --query "proposalIds[0]" `
    --output tsv

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client $clientName

# Vote on the proposed cce policy.
$proposalId = az cleanroom governance deployment policy show `
    --contract-id $contractId `
    --governance-client $clientName `
    --query "proposalIds[0]" `
    --output tsv

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client $clientName

# Vote on the enable logging proposal.
$proposalId = az cleanroom governance contract runtime-option get `
    --option logging `
    --contract-id $contractId `
    --governance-client $clientName `
    --query "proposalIds[0]" `
    --output tsv

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client $clientName

# Vote on the enable telemetry proposal.
$proposalId = az cleanroom governance contract runtime-option get `
    --option telemetry `
    --contract-id $contractId `
    --governance-client $clientName `
    --query "proposalIds[0]" `
    --output tsv

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client $clientName

$clientName = "ob-consumer-client"
pwsh $collabSamplePath/verify-deployment-proposals.ps1 `
    -cleanroomConfig $consumerConfig `
    -governanceClient $clientName

# Vote on the proposed deployment template.
$proposalId = az cleanroom governance deployment template show `
    --contract-id $contractId `
    --governance-client $clientName `
    --query "proposalIds[0]" `
    --output tsv

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client $clientName

# Vote on the proposed cce policy.
$proposalId = az cleanroom governance deployment policy show `
    --contract-id $contractId `
    --governance-client $clientName `
    --query "proposalIds[0]" `
    --output tsv

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client $clientName

# Vote on the enable logging proposal.
$proposalId = az cleanroom governance contract runtime-option get `
    --option logging `
    --contract-id $contractId `
    --governance-client $clientName `
    --query "proposalIds[0]" `
    --output tsv

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client $clientName

# Vote on the enable telemetry proposal.
$proposalId = az cleanroom governance contract runtime-option get `
    --option telemetry `
    --contract-id $contractId `
    --governance-client $clientName `
    --query "proposalIds[0]" `
    --output tsv

az cleanroom governance proposal vote `
    --proposal-id $proposalId `
    --action accept `
    --governance-client $clientName

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# $db below refers to the output of deploy-postgresql.ps1.
# wrap-secret keeps the DB password as a secret in AKV.
$passwordKid = "wrapped-db-password"
$wrapResult = (az cleanroom config wrap-secret `
        --name $passwordKid `
        --value $db.password `
        --secret-key-vault $pubResult.dek.kv.id `
        --contract-id $contractId `
        --cleanroom-config $publisherConfig `
        --secretstore-config $publisherSecretStoreConfig `
        --kek-secretstore-name publisher-kek-store `
        --governance-client "ob-publisher-client" | ConvertFrom-Json)

# Get details about the MI that should be used to access the wrapped secret.
$identity = (az resource show --id $pubResult.mi.id --query properties | ConvertFrom-Json)

$secretConfig = @{
    dbEndpoint = $db.endpoint
    dbName     = $db.name
    dbUser     = $db.user
    dbPassword = @{
        clientId    = $identity.clientId
        tenantId    = $identity.tenantId
        kid         = $wrapResult.kid
        akvEndpoint = $wrapResult.akvEndpoint
        kek         = @{
            kid         = $wrapResult.kek.kid
            akvEndpoint = $wrapResult.kek.akvEndpoint
            maaEndpoint = $pubResult.maa_endpoint
        }
    }
} | ConvertTo-Json | base64 -w 0

Write-Host "DB secret configuration:"
$secretConfig | base64 -d

# Below keeps the DB configuration as a secret in CCF (option 1).
az cleanroom governance contract secret set `
    --secret-name $DB_CONFIG_CGS_SECRET_NAME `
    --value $secretConfig `
    --contract-id $contractId `
    --governance-client "ob-publisher-client"

$usePreprovisionedOIDC = $false
if ($env:USE_PREPROVISIONED_OIDC -eq "true") {
    $usePreprovisionedOIDC = $true
}

Write-Host "usePreprovisionedOIDC:$usePreprovisionedOIDC"

# Creates a KEK with SKR policy, wraps DEKs with the KEK and put in kv.
az cleanroom config wrap-deks `
    --contract-id $contractId `
    --cleanroom-config $publisherConfig `
    --datastore-config $publisherDatastoreConfig `
    --secretstore-config $publisherSecretStoreConfig `
    --governance-client "ob-publisher-client"

# Setup OIDC issuer and managed identity access to storage/KV in publisher tenant.
pwsh $collabSamplePath/setup-access.ps1 `
    -resourceGroup $publisherResourceGroup `
    -contractId $contractId  `
    -outDir $outDir `
    -kvType akvpremium `
    -governanceClient "ob-publisher-client" `
    -usePreprovisionedOIDC:$usePreprovisionedOIDC

# Creates a KEK with SKR policy, wraps DEKs with the KEK and put in kv.
az cleanroom config wrap-deks `
    --contract-id $contractId `
    --cleanroom-config $consumerConfig `
    --datastore-config $consumerDatastoreConfig `
    --secretstore-config $consumerSecretStoreConfig `
    --governance-client "ob-consumer-client"

# Setup OIDC issuer endpoint and managed identity access to storage/KV in consumer tenant.
pwsh $collabSamplePath/setup-access.ps1 `
    -resourceGroup $consumerResourceGroup `
    -contractId $contractId `
    -outDir $outDir `
    -kvType akvpremium `
    -governanceClient "ob-consumer-client" `
    -usePreprovisionedOIDC:$usePreprovisionedOIDC
