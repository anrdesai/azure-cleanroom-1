$uid = id -u ${env:USER}
$gid = id -g ${env:USER}
docker run `
    -u ${uid}:${gid} `
    -v $PSScriptRoot/../templates:/input `
    -v $PSScriptRoot/../models:/output `
    --name datamodel-codegen `
    --rm koxudaxi/datamodel-code-generator `
    --input /input/cleanroomspec-openapi.yaml --output /output/model.py


$header = @"
# DO NOT EDIT. AUTO-GENERATED CODE.
# Please run tools/generate_models.ps1 to re-generate the file after editing cleanroomspec-openapi.yaml.
"@

$model = Get-Content -Path $PSScriptRoot/../models/model.py
Set-Content $PSScriptRoot/../models/model.py -Value $header, $([environment]::NewLine), $model

docker run `
    -v $PSScriptRoot/../models:/src `
    --workdir /src `
    --name black-formatter `
    --rm pyfound/black:latest_release black . -t py311