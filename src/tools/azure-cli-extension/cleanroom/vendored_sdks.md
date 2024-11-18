# `vendored_sdks` Code Generation

- [`vendored_sdks` Code Generation](#vendored_sdks-code-generation)
  - [Generating `vendored_sdks/confidentialledger` code](#generating-vendored_sdksconfidentialledger-code)
  - [Generating `vendored_sdks/storage` code](#generating-vendored_sdksstorage-code)
  - [Generating `vendored_sdks/keyvault` code](#generating-vendored_sdkskeyvault-code)

Steps below inspired from https://github.com/Azure/azure-cli-extensions/blob/main/src/scvmm/setup.md.

## Generating `vendored_sdks/confidentialledger` code
```powershell
# Clone the azure-rest-api-specs is some directory.
git clone https://github.com/Azure/azure-rest-api-specs.git .
$specs=<path to above cloned repo>

sudo npm i -g autorest

$pythonSdksFolder="./sdks"

autorest `
  $specs/specification/confidentialledger/resource-manager/readme.md `
  --track2 `
  --python `
  --python-sdks-folder=$pythonSdksFolder `
  --python-mode=update `
  --version=3.7.4 `
  --use=@autorest/python@~5.12.0 `
  --use=@autorest/modelerfour@~4.19.3

$root=$(git rev-parse --show-toplevel)
$azext="$root/src/tools/azure-cli-extension/cleanroom/azext_cleanroom"

# Remove the existing sdk.
rm -rf $azext/vendored_sdks/confidentialledger

# Replace the old sdk by the newly generated sdk.
mv $pythonSdksFolder/confidentialledger/azure-mgmt-confidentialledger/azure/mgmt/confidentialledger $azext/vendored_sdks

rm -rf $pythonSdksFolder
```

## Generating `vendored_sdks/storage` code
```powershell
# Clone the azure-rest-api-specs is some directory.
git clone https://github.com/Azure/azure-rest-api-specs.git .
$specs=<path to above cloned repo>

sudo npm i -g autorest

$pythonSdksFolder="./sdks"

autorest `
  $specs/specification/storage/resource-manager/readme.md `
  --track2 `
  --python `
  --python-sdks-folder=$pythonSdksFolder `
  --python-mode=update `
  --version=3.7.4 `
  --use=@autorest/python@~5.12.0 `
  --use=@autorest/modelerfour@~4.19.3

$root=$(git rev-parse --show-toplevel)
$azext="$root/src/tools/azure-cli-extension/cleanroom/azext_cleanroom"

# Remove the existing sdk.
rm -rf $azext/vendored_sdks/storage

# Replace the old sdk by the newly generated sdk.
# At the time of running these steps v2023_05_01 was the latest version. Update as required.
mv $pythonSdksFolder/storage/azure-mgmt-storage/azure/mgmt/storage/v2023_05_01 $azext/vendored_sdks/storage

rm -rf $pythonSdksFolder
```

## Generating `vendored_sdks/keyvault` code
```powershell
# Clone the azure-rest-api-specs is some directory.
git clone https://github.com/Azure/azure-rest-api-specs.git .
$specs=<path to above cloned repo>

sudo npm i -g autorest

$pythonSdksFolder="./sdks"

autorest `
  $specs/specification/keyvault/resource-manager/readme.md `
  --track2 `
  --python `
  --python-sdks-folder=$pythonSdksFolder `
  --python-mode=update `
  --version=3.7.4 `
  --use=@autorest/python@~5.12.0 `
  --use=@autorest/modelerfour@~4.19.3

$root=$(git rev-parse --show-toplevel)
$azext="$root/src/tools/azure-cli-extension/cleanroom/azext_cleanroom"

# Remove the existing sdk.
rm -rf $azext/vendored_sdks/keyvault

# Replace the old sdk by the newly generated sdk.
# At the time of running these steps v2023_07_01 was the latest version. Update as required.
mv $pythonSdksFolder/keyvault/azure-mgmt-keyvault/azure/mgmt/keyvault/v2023_07_01 $azext/vendored_sdks/keyvault

rm -rf $pythonSdksFolder
```