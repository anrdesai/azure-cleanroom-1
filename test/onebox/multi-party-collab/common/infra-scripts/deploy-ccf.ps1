function Deploy-Ccf {
    [CmdletBinding()]
    param (
        [string]$resourceGroup,
        [string]$ccfName,
        [string]$location,
        [string]$storageAccountName,
        [string]$initialMemberName,
        [string]$memberCertPath,
        [string]$outDir = ""
    )

    if ($outDir -eq "") {
        $outDir = "$($MyInvocation.PSScriptRoot)/sandbox_common"
    }

    az storage account create `
        --name $storageAccountName `
        --resource-group $resourceGroup `
        --allow-shared-key-access true

    $subscriptionId = az account show --query id --output tsv

    $storageAccountId = az storage account show `
        --name $storageAccountName `
        --resource-group $resourceGroup `
        --query "id" `
        --output tsv
    $operatorName = "operator"
    if (!(Test-Path "$outDir/${operatorName}_cert.pem")) {
        az cleanroom governance member keygenerator-sh | bash -s -- --name $operatorName --gen-enc-key --out $outDir
    }

    @"
[{
    "certificate": "$outDir/${operatorName}_cert.pem",
    "encryptionPublicKey": "$outDir/${operatorName}_enc_pubk.pem",
    "memberData": {
        "identifier": "$operatorName",
        "is_operator": true
    }
},
{
    "certificate": "$memberCertPath",
    "memberData": {
        "identifier": "$initialMemberName"
    }
}]
"@ >  $outDir/members.json

    @"
{
    "location": "$location",
    "subscriptionId": "$subscriptionId",
    "resourceGroupName": "$resourceGroup",
    "azureFiles": {
        "storageAccountId": "$storageAccountId"
    }
}
"@ >  $outDir/providerConfig.json

    az cleanroom ccf provider deploy
    # Create the CCF network and activate operator membership.
    Write-Host "Starting deployment of CCF $ccfName on CACI in RG $resourceGroup"
    $response = (az cleanroom ccf network create `
            --name $ccfName `
            --members $outDir/members.json `
            --provider-config $outDir/providerConfig.json | ConvertFrom-Json)
    $ccfEndpoint = $response.endpoint
    $response = (curl "$ccfEndpoint/node/network" -k --silent | ConvertFrom-Json)
    # Trimming an extra new-line character added to the cert.
    $response.service_certificate.TrimEnd("`n") | Out-File "$outDir/service_cert.pem"

    # Deploy the governance client for the operator to take any gov actions.
    $cgsProjectName = "ccf-network-governance"
    az cleanroom governance client deploy `
        --ccf-endpoint $ccfEndpoint `
        --signing-key $outDir/${operatorName}_privk.pem `
        --signing-cert $outDir/${operatorName}_cert.pem `
        --service-cert $outDir/service_cert.pem `
        --name $cgsProjectName

    # Activate the operator membership by default in the cluster that just got created.
    az cleanroom governance member activate --governance-client $cgsProjectName

    # Configure the ccf provider client for the operator to take any operator actions like opening
    # the network.
    az cleanroom ccf provider configure `
        --signing-key $outDir/${operatorName}_privk.pem `
        --signing-cert $outDir/${operatorName}_cert.pem

    # Open the network as the operator.
    az cleanroom ccf network transition-to-open `
        --name $ccfName `
        --provider-config $outDir/providerConfig.json
}
