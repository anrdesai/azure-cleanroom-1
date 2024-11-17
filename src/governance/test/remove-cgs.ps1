[CmdletBinding()]
param
(
)

docker compose -f $PSScriptRoot/docker-compose.yml down
rm -rf $PSScriptRoot/sandbox_common

