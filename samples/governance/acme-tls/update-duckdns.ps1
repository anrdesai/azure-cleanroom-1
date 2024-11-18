param(
    [string]
    $domain = "cgssvc",
    [string]
    $cg = "ccf-acme",
    [string]
    $rg = "gsinha-test-rg",
    [string]
    $token
)

$ccfip=$(az container show --name $cg --resource-group $rg | jq -r .ipAddress.ip)
Write-Host "Updating to $ccfip"

if ($duckdns)
{
    # URL via duckdns is: cgssvc.duckdns.org
    # Eg: curl "https://www.duckdns.org/update?domains=cgssvc&token={someguid}&ip=4.175.98.175&verbose=true"
    curl "https://www.duckdns.org/update?domains=${domain}&token=${token}&ip=${ccfip}&verbose=true"
}