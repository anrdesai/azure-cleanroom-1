[CmdletBinding()]
param
(
    [string]
    $outDir = "$PSScriptRoot/generated",

    [string]
    $ccfEndpoint = "https://host.docker.internal:9081",

    [string]
    $serviceCert = "$PSScriptRoot/generated/ccf/service_cert.pem",

    [string]
    $contractId = "collab1",

    [switch]
    $y,

    [ValidateSet('mcr', 'local')]
    [string]$registry = "mcr"
)

# This script assumes a CCF instance was deployed in docker with the initial member that acts as the
# consumer for the multi-party collab sample.
$root = git rev-parse --show-toplevel
$collabSamplePath = "$root/samples/multi-party-collab"

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

$resourceGroupTags = ""
if ($env:GITHUB_ACTIONS -eq "true") {
    $publisherResourceGroup = "cl-ob-publisher-${env:RUN_ID}"
    $consumerResourceGroup = "cl-ob-consumer-${env:RUN_ID}"
    $resourceGroupTags = "github_actions=multi-party-collab-${env:RUN_ID}"
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
if (-not (Test-Path -Path "$outDir/ccf/publisher_cert.pem")) {
    az cleanroom governance member keygenerator-sh | bash -s -- --name "publisher" --out "$outDir/ccf"
}

# Invite publisher to the consortium.
$publisherTenantId = az account show --query "tenantId" --output tsv

# "consumer" member makes a proposal for adding the new member "publisher".
$proposalId = (az cleanroom governance member add `
        --certificate $outDir/ccf/publisher_cert.pem `
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
    --signing-cert $outDir/ccf/publisher_cert.pem `
    --signing-key $outDir/ccf/publisher_privk.pem `
    --service-cert $outDir/ccf/service_cert.pem `
    --name "ob-publisher-client"

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# "publisher" accepts the invitation and becomes an active member in the consortium.
az cleanroom governance member activate --governance-client "ob-publisher-client"

az cleanroom config init --cleanroom-config $publisherConfig

# Create storage account, KV and MI resources.
pwsh $collabSamplePath/prepare-resources.ps1 `
    -resourceGroup $publisherResourceGroup `
    -resourceGroupTags $resourceGroupTags `
    -kvType akvpremium `
    -outDir $outDir
$result = Get-Content "$outDir/$publisherResourceGroup/resources.generated.json" | ConvertFrom-Json
# if (!$y) {
#     Read-Host "prepare-resources done. Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# Create a KEK entry in the configuration.
az cleanroom config set-kek `
    --kek-key-vault $result.kek.kv.id `
    --maa-url $result.maa_endpoint `
    --cleanroom-config $publisherConfig
# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

# Create a datasource entry in the configuration.
az cleanroom config add-datasource `
    --cleanroom-config $publisherConfig `
    --name publisher-input `
    --storage-account $result.sa.id `
    --identity $result.mi.id `
    --dek-key-vault $result.dek.kv.id

# Encrypt and upload content.
az cleanroom datasource upload `
    --cleanroom-config $publisherConfig `
    --name publisher-input `
    --dataset-folder $collabSamplePath/publisher-demo/publisher-input

# $result below refers to the output of the prepare-resources.ps1 that was run earlier.
az cleanroom config set-logging `
    --cleanroom-config $publisherConfig `
    --storage-account $result.sa.id `
    --identity $result.mi.id `
    --dek-key-vault $result.dek.kv.id

az cleanroom config set-telemetry `
    --cleanroom-config $publisherConfig `
    --storage-account $result.sa.id `
    --identity $result.mi.id `
    --dek-key-vault $result.dek.kv.id

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

az cleanroom config init --cleanroom-config $consumerConfig

# Create storage account, KV and MI resources.
pwsh $collabSamplePath/prepare-resources.ps1 `
    -resourceGroup $consumerResourceGroup `
    -resourceGroupTags $resourceGroupTags `
    -kvType akvpremium `
    -outDir $outDir
$result = Get-Content "$outDir/$consumerResourceGroup/resources.generated.json" | ConvertFrom-Json

# Create a KEK entry in the configuration.
az cleanroom config set-kek `
    --kek-key-vault $result.kek.kv.id `
    --maa-url $result.maa_endpoint `
    --cleanroom-config $consumerConfig

# Create a datasink entry in the configuration.
az cleanroom config add-datasink `
    --cleanroom-config $consumerConfig `
    --name consumer-output `
    --storage-account $result.sa.id `
    --identity $result.mi.id `
    --dek-key-vault $result.dek.kv.id

$sample_code = $(cat $collabSamplePath/consumer-demo/application/main.go | base64 -w 0)
az cleanroom config add-application `
    --cleanroom-config $consumerConfig `
    --name demo-app `
    --image "docker.io/golang@sha256:f43c6f049f04cbbaeb28f0aad3eea15274a7d0a7899a617d0037aec48d7ab010" `
    --command "bash -c 'echo `$CODE | base64 -d > main.go; go run main.go'" `
    --mounts "src=publisher-input,dst=/mnt/remote/input" `
    "src=consumer-output,dst=/mnt/remote/output" `
    --env-vars OUTPUT_LOCATION=/mnt/remote/output `
    INPUT_LOCATION=/mnt/remote/input `
    CODE="$sample_code" `
    --cpu 0.5 `
    --memory 4

# Generate the cleanroom config which contains all the datasources, sinks and applications that are
# configured by both the producer and consumer.
az cleanroom config view `
    --cleanroom-config $consumerConfig `
    --configs $publisherConfig > $outDir/configurations/cleanroom-config

# if (!$y) {
#     Read-Host "Press Enter to continue or Ctrl+C to quit" | Out-Null
# }

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
    --allow-all

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

# Creates a KEK with SKR policy, wraps DEKs with the KEK and put in kv.
az cleanroom config wrap-deks `
    --contract-id $contractId `
    --cleanroom-config $publisherConfig `
    --governance-client "ob-publisher-client"

# Setup OIDC issuer and managed identity access to storage/KV in publisher tenant.
pwsh $collabSamplePath/setup-access.ps1 `
    -resourceGroup $publisherResourceGroup `
    -contractId $contractId  `
    -outDir $outDir `
    -governanceClient "ob-publisher-client"

# Creates a KEK with SKR policy, wraps DEKs with the KEK and put in kv.
az cleanroom config wrap-deks `
    --contract-id $contractId `
    --cleanroom-config $consumerConfig `
    --governance-client "ob-consumer-client"

# Setup OIDC issuer endpoint and managed identity access to storage/KV in consumer tenant.
pwsh $collabSamplePath/setup-access.ps1 `
    -resourceGroup $consumerResourceGroup `
    -contractId $contractId `
    -outDir $outDir `
    -governanceClient "ob-consumer-client"
