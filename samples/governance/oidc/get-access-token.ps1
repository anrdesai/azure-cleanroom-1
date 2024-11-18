[CmdletBinding()]
param
(
  [string]
  $scope = "https://vault.azure.net/.default",
  [string]
  $clientId = "df06c73e-f31d-4573-9f08-6225548b4ca1",
  [string]
  $tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
  [string]
  [Parameter(Mandatory)]
  $clientAssertion
)

# https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-client-creds-grant-flow#third-case-access-token-request-with-a-federated-credential
$endpoint = "https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/token";
$client_assertion_type = "urn%3Aietf%3Aparams%3Aoauth%3Aclient-assertion-type%3Ajwt-bearer";
$scope = [uri]::EscapeDataString($scope);
$data = "scope=${scope}&client_id=${clientId}&client_assertion_type=${client_assertion_type}&client_assertion=${clientAssertion}&grant_type=client_credentials";
curl -sS -X POST -H "content-type: application/x-www-form-urlencoded" -d $data ${endpoint} | jq