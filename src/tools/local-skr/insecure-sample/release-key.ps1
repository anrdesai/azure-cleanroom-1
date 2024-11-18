[CmdletBinding()]
param
(
    [string]
    [Parameter(Mandatory)]
    $keyName,

    [string]
    $mhsmName,

    [string]
    $vaultName
)

$akvEndpoint = ""
if ($mhsmName -ne "") {
    $akvEndpoint = "$mhsmName.managedhsm.azure.net"
}
elseif ($vaultName -ne "") {
    $akvEndpoint = "$vaultName.vault.azure.net"
}
else {
    throw "Either mshsName or vaultName must be specified"
}

$accessToken = (az account get-access-token --resource https://vault.azure.net --query accessToken --output tsv)
$content = @"
{
    "akv_endpoint": "$akvEndpoint",
    "maa_endpoint": "sharedneu.neu.attest.azure.net",
    "kid": "$keyName",
    "access_token": "$accessToken"
}
"@
(curl -sS -X POST "http://localhost:8284/key/release" `
    -H "content-type: application/json" `
    -d $content | ConvertFrom-Json | ConvertTo-Json)

# $maaToken = (curl -sS -X POST "https://sharedneu.neu.attest.azure.net/attest/SevSnpVm?api-version=2022-08-01" `
#         -d "@${PSScriptRoot}/maa-request.json" `
#         -H "content-type: application/json" | ConvertFrom-Json).token
# curl -X POST "https://${mhsmName}.vault.azure.net/keys/${keyName}/release?api-version=7.3" `
#     -H "content-type: application/json" `
#     -H "Authorization: Bearer $accessToken" `
#     -d "{'target': '$maaToken'}"