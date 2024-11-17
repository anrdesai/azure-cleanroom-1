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
  $contractId = "collab1",

  [switch]
  $y,

  [ValidateSet('mcr', 'local')]
  [string]$registry = "local",

  [string]$registryUrl = "localhost:5001",

  [string]$registryTag = "latest",

  [string]
  $cleanroomClientEndpoint = "localhost:8321",

  [switch]
  $caci
)

#https://learn.microsoft.com/en-us/powershell/scripting/learn/experimental-features?view=powershell-7.4#psnativecommanderroractionpreference
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

# This script assumes a CCF instance was deployed in docker with the initial member that acts as the
# consumer for the multi-party collab sample.
$root = git rev-parse --show-toplevel

if ($ccfOutDir -eq "") {
  $ccfOutDir = "$root/test/onebox/multi-party-collab/generated/ccf"
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
mkdir -p "$datastoreOutdir"
$publisherDatastoreConfig = "$datastoreOutdir/encrypted-storage-cleanroom-client-publisher-datastore-config"
$consumerDatastoreConfig = "$datastoreOutdir/encrypted-storage-cleanroom-client-consumer-datastore-config"

mkdir -p "$datastoreOutdir/secrets"
$publisherSecretStoreConfig = "$datastoreOutdir/secrets/encrypted-storage-cleanroom-client-publisher-secretstore-config"
$consumerSecretStoreConfig = "$datastoreOutdir/secrets/encrypted-storage-cleanroom-client-consumer-secretstore-config"

$publisherLocalSecretStore = "$datastoreOutdir/secrets/encrypted-storage-cleanroom-client-publisher-secretstore-local"
$consumerLocalSecretStore = "$datastoreOutdir/secrets/encrypted-storage-cleanroom-client-consumer-secretstore-local"

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
  $tag = cat "$ccfOutDir/local-registry-tag.txt"
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
pwsh $PSScriptRoot/../../prepare-resources.ps1 `
  -resourceGroup $publisherResourceGroup `
  -resourceGroupTags $resourceGroupTags `
  -kvType akvpremium `
  -outDir $outDir
$result = Get-Content "$outDir/$publisherResourceGroup/resources.generated.json" | ConvertFrom-Json

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-secretstore `
  -H "content-type: application/json" `
  -d @"
{
    "name": "publisher-local-store",
    "configName": "$publisherSecretStoreConfig",
    "backingStoreType": "Local_File",
    "backingStorePath": "$publisherLocalSecretStore"
}
"@

$addDatastoreRequest = @"
{
  "configName": "$publisherDatastoreConfig",
  "name": "publisher-input",
  "secretStore": "publisher-local-store",
  "secretStoreConfig": "$publisherSecretStoreConfig",
  "encryptionMode": "CPK",
  "backingStoreType": "Azure_BlobStorage",
  "backingStoreId": "$($result.sa.id)"
}
"@
curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-datastore `
  -H "content-type: application/json" `
  -d $addDatastoreRequest

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/upload-datastore `
  -H "content-type: application/json" `
  -d @"
{
    "name": "publisher-input",
    "configName": "$publisherDatastoreConfig",
    "src": "$PSScriptRoot/../publisher/input"
}
"@

# if (!$y) {
#     Read-Host "prepare-resources done. Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-secretstore `
  -H "content-type: application/json" `
  -d @"
{
    "name": "publisher-dek-store",
    "configName": "$publisherSecretStoreConfig",
    "backingStoreType": "Azure_KeyVault",
    "backingStoreId": "$($result.dek.kv.id)"
}
"@

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-secretstore `
  -H "content-type: application/json" `
  -d @"
{
    "name": "publisher-kek-store",
    "configName": "$publisherSecretStoreConfig",
    "backingStoreType": "Azure_KeyVault_Managed_HSM",
    "backingStoreId": "$($result.kek.kv.id)",
    "attestationEndpoint": "$($result.maa_endpoint)"
}
"@

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/init `
  -H "content-type: application/json" `
  -d @"
{
  "configName": "$publisherConfig"
}
"@

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

$identity = $(az resource show --ids $result.mi.id --query "properties") | ConvertFrom-Json

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-identity-az-federated `
  -H "content-type: application/json" `
  -d @"
{
    "configName": "$publisherConfig",
    "name": "publisher-identity",
    "clientId": "$($identity.clientId)",
    "tenantId": "$($identity.tenantId)"
}
"@

# Create a datasource entry in the configuration.
curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/add-datasource -H "content-type: application/json" `
  -d @"
{
    "datastoreName": "publisher-input",
    "datastoreConfigName": "$publisherDatastoreConfig",
    "secretStoreConfig": "$publisherSecretStoreConfig",
    "dekSecretStore": "publisher-dek-store",
    "kekSecretStore": "publisher-kek-store",
    "identity": "publisher-identity",
    "configName": "$publisherConfig"
}
"@

$containerSuffix = $($($(New-Guid).Guid) -replace '-').ToLower()
Write-Host "Using container suffix {$containerSuffix} for application-telemetry"

# $result below refers to the output of the prepare-resources.ps1 that was run earlier.
curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/set-logging -H "content-type: application/json" `
  -d @"
{
    "storageAccountId": "$($result.sa.id)",
    "identity": "publisher-identity",
    "keyVault": "$($result.dek.kv.id)",
    "configName": "$publisherConfig",
    "datastoreConfigName": "$publisherDatastoreConfig",
    "secretStoreConfig": "$publisherSecretStoreConfig",
    "dekSecretStore": "publisher-dek-store",
    "kekSecretStore": "publisher-kek-store",
    "datastoreSecretStore": "publisher-local-store",
    "encryptionMode": "CPK",
    "containerSuffix": "$containerSuffix"
}
"@

$containerSuffix = $($($(New-Guid).Guid) -replace '-').ToLower()
Write-Host "Using container suffix {$containerSuffix} for infrastructure-telemetry"

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/set-telemetry -H "content-type: application/json" `
  -d @"
{
    "storageAccountId": "$($result.sa.id)",
    "identity": "publisher-identity",
    "keyVault": "$($result.dek.kv.id)",
    "configName": "$publisherConfig",
    "datastoreConfigName": "$publisherDatastoreConfig",
    "secretStoreConfig": "$publisherSecretStoreConfig",
    "dekSecretStore": "publisher-dek-store",
    "kekSecretStore": "publisher-kek-store",
    "datastoreSecretStore": "publisher-local-store",
    "encryptionMode": "CPK",
    "containerSuffix": "$containerSuffix"
}
"@

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# Create storage account, KV and MI resources.
pwsh $PSScriptRoot/../../prepare-resources.ps1 `
  -resourceGroup $consumerResourceGroup `
  -resourceGroupTags $resourceGroupTags `
  -kvType akvpremium `
  -outDir $outDir
$result = Get-Content "$outDir/$consumerResourceGroup/resources.generated.json" | ConvertFrom-Json

$containerName = $($($(New-Guid).Guid) -replace '-').ToLower()
Write-Host "Using container name {$containerName} for datastore {consumer-output}"

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-secretstore `
  -H "content-type: application/json" `
  -d @"
{
  "name": "consumer-local-store",
  "configName": "$consumerSecretStoreConfig",
  "backingStoreType": "Local_File",
  "backingStorePath": "$consumerLocalSecretStore"
}
"@

$containerName = $($($(New-Guid).Guid) -replace '-').ToLower()
Write-Host "Using container name {$containerName} for datastore {consumer-output}"

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-datastore `
  -H "content-type: application/json" `
  -d @"
{
  "configName": "$consumerDatastoreConfig",
  "name": "consumer-output",
  "secretStore": "consumer-local-store",
  "secretStoreConfig": "$consumerSecretStoreConfig",
  "encryptionMode": "CPK",
  "backingStoreType": "Azure_BlobStorage",
  "backingStoreId": "$($result.sa.id)",
  "containerName": "$containerName"
}
"@


curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-secretstore `
  -H "content-type: application/json" `
  -d @"
{
    "name": "consumer-dek-store",
    "configName": "$consumerSecretStoreConfig",
    "backingStoreType": "Azure_KeyVault",
    "backingStoreId": "$($result.dek.kv.id)"
}
"@

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-secretstore `
  -H "content-type: application/json" `
  -d @"
{
    "name": "consumer-kek-store",
    "configName": "$consumerSecretStoreConfig",
    "backingStoreType": "Azure_KeyVault_Managed_HSM",
    "backingStoreId": "$($result.kek.kv.id)",
    "attestationEndpoint": "$($result.maa_endpoint)"
}
"@

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/init `
  -H "content-type: application/json" `
  -d @"
{
  "configName": "$consumerConfig"
}
"@


$identity = $(az resource show --ids $result.mi.id --query "properties") | ConvertFrom-Json

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/add-identity-az-federated `
  -H "content-type: application/json" `
  -d @"
{
    "configName": "$consumerConfig",
    "name": "consumer-identity",
    "clientId": "$($identity.clientId)",
    "tenantId": "$($identity.tenantId)"
}
"@

# Create a datasink entry in the configuration.
curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/add-datasink -H "content-type: application/json" `
  -d @"
{
    "datastoreName": "consumer-output",
    "datastoreConfigName": "$consumerDatastoreConfig",
    "identity": "consumer-identity",
    "secretStoreConfig": "$consumerSecretStoreConfig",
    "dekSecretStore": "consumer-dek-store",
    "kekSecretStore": "consumer-kek-store",
    "configName": "$consumerConfig"
}
"@

$sample_code = $(cat $PSScriptRoot/../consumer/application/main.go | base64 -w 0)
curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/add-application -H "content-type: application/json" -d @"
{
    "name": "demo-app",
    "image": "docker.io/golang@sha256:f43c6f049f04cbbaeb28f0aad3eea15274a7d0a7899a617d0037aec48d7ab010",
    "command": "bash -c 'echo `$CODE | base64 -d > main.go; go run main.go'",
    "mounts": ["src=publisher-input,dst=/mnt/remote/input", "src=consumer-output,dst=/mnt/remote/output"],
    "environmentVariables": ["OUTPUT_LOCATION=/mnt/remote/output", "INPUT_LOCATION=/mnt/remote/input", "CODE=$sample_code"],
    "cpu": "2",
    "memory": "4",
    "configName": "$consumerConfig"
}
"@

# Generate the cleanroom config which contains all the datasources, sinks and applications that are
# configured by both the producer and consumer.
curl -X POST $cleanroomClientEndpoint/config/view `
  -H "content-type: application/json" `
  -d @"
{
  "configName": "$consumerConfig",
  "configs": ["$publisherConfig"],
  "outputFile": "$outDir/configurations/cleanroom-config"
}
"@

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

$validateConfigRequest = @"
{
  "configName": "$outDir/configurations/cleanroom-config"
}
"@
curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/validate `
  -H "content-type: application/json" `
  -d $validateConfigRequest

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
  $env:AZCLI_CLEANROOM_CONTAINER_REGISTRY_URL = $registryUrl
  $env:AZCLI_CLEANROOM_SIDECARS_VERSIONS_DOCUMENT_URL = "${registryUrl}/sidecar-digests:$registryTag"
}

if ($caci) {
  az cleanroom governance deployment generate `
    --contract-id $contractId `
    --governance-client "ob-consumer-client" `
    --output-dir $outDir/deployments
}
else {
  az cleanroom governance deployment generate `
    --contract-id $contractId `
    --governance-client "ob-consumer-client" `
    --output-dir $outDir/deployments `
    --security-policy-creation-option allow-all
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
pwsh $PSScriptRoot/../../verify-deployment-proposals.ps1 `
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
pwsh $PSScriptRoot/../../verify-deployment-proposals.ps1 `
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

# Creates a KEK with SKR policy, wraps DEKs with the KEK and put in kv.
$encodedClPolicy = (az cleanroom governance deployment policy show `
    --contract-id $contractId `
    --governance-client "ob-publisher-client" | base64 -w 0)
curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/create-kek -H "content-type: application/json" -d @"
{
    "contractId": "$contractId",
    "cleanroomPolicy": "$encodedClPolicy",
    "configName": "$publisherConfig",
    "secretStoreConfig": "$publisherSecretStoreConfig"
}
"@

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/wrap-deks -H "content-type: application/json" -d @"
{
    "contractId": "$contractId",
    "configName": "$publisherConfig",
    "datastoreConfigName": "$publisherDatastoreConfig",
    "secretStoreConfig": "$publisherSecretStoreConfig"
}
"@

$usePreprovisionedOIDC = $false
if ($env:USE_PREPROVISIONED_OIDC -eq "true") {
  $usePreprovisionedOIDC = $true
}
# Setup OIDC issuer and managed identity access to storage/KV in publisher tenant.
pwsh $PSScriptRoot/../../setup-access.ps1 `
  -resourceGroup $publisherResourceGroup `
  -contractId $contractId  `
  -outDir $outDir `
  -kvType akvpremium `
  -governanceClient "ob-publisher-client" `
  -usePreprovisionedOIDC:$usePreprovisionedOIDC

# Creates a KEK with SKR policy, wraps DEKs with the KEK and put in kv.
curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/create-kek -H "content-type: application/json" -d @"
{
    "contractId": "$contractId",
    "cleanroomPolicy": "$encodedClPolicy",
    "configName": "$consumerConfig",
    "secretStoreConfig": "$consumerSecretStoreConfig"
}
"@

curl --fail-with-body `
  -w "\n%{method} %{url} completed with %{response_code}\n" `
  -X POST $cleanroomClientEndpoint/config/wrap-deks -H "content-type: application/json" -d @"
{
    "contractId": "$contractId",
    "configName": "$consumerConfig",
    "datastoreConfigName": "$consumerDatastoreConfig",
    "secretStoreConfig": "$consumerSecretStoreConfig"
}
"@

# Setup OIDC issuer endpoint and managed identity access to storage/KV in consumer tenant.
pwsh $PSScriptRoot/../../setup-access.ps1 `
  -resourceGroup $consumerResourceGroup `
  -contractId $contractId `
  -outDir $outDir `
  -kvType akvpremium `
  -governanceClient "ob-consumer-client" `
  -usePreprovisionedOIDC:$usePreprovisionedOIDC
