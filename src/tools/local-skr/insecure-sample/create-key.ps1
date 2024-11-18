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

$policyJson = @"
{
    "anyOf": [
        {
            "allOf": [
                {
                    "claim": "x-ms-sevsnpvm-hostdata",
                    "equals": "73973b78d70cc68353426de188db5dfc57e5b766e399935fb73a61127ea26d20"
                },
                {
                    "claim": "x-ms-compliance-status",
                    "equals": "azure-compliant-uvm"
                },
                {
                    "claim": "x-ms-attestation-type",
                    "equals": "sevsnpvm"
                }
            ],
            "authority": "https://sharedneu.neu.attest.azure.net"
        }
    ],
    "version": "1.0.0"
}
"@

if ($mhsmName -ne "") {
    az keyvault key create `
        --name $keyName `
        --policy $policyJson `
        --kty RSA `
        --hsm-name $mhsmName `
        --exportable $true `
        --protection hsm `
        --immutable true
}
elseif ($vaultName -ne "") {
    az keyvault key create `
        --name $keyName `
        --policy $policyJson `
        --kty RSA `
        --vault-name $vaultName `
        --exportable $true `
        --protection hsm `
        --immutable true
}
else {
    throw "Either mshsName or vaultName must be specified"
}

