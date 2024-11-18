# AES Encryptor invocation sample from Python
A simple go module that exposes `EncryptChunk` method to showcase how to invoke the same from Python via the `az cleanroom` cli. To try this sample follow the below steps:
```powershell
$root=$(git rev-parse --show-toplevel)
cd $root/src/tools/aes_encryptor

# Build aes_encryptor.so
go build -o aes_encryptor.so -buildmode=c-shared main.go

# Drop aes_encryptor.so into the binaries folder and build to pack it in with the cli extension.
mv ./aes_encryptor.so $root/src/tools/azure-cli-extension/cleanroom/azext_cleanroom/binaries
../../../build/build-azcliext-cleanroom.ps1

# Invoke the sample command to demonstrate API invocation.
az cleanroom datastore encrypt
```

## Links
- https://medium.com/analytics-vidhya/running-go-code-from-python-a65b3ae34a2d
- https://fluhus.github.io/snopher/
