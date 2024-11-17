[CmdletBinding()]
param
(
    [string]
    $ccfProjectName,

    [string]
    $projectName
)

docker compose -f $PSScriptRoot/docker-compose.yml -p $ccfProjectName down
az cleanroom governance client remove --name $projectName
rm -rf $PSScriptRoot/sandbox_common

