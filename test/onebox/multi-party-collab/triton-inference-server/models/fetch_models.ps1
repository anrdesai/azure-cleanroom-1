# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# ONNX densenet
if (!(Test-Path $PSScriptRoot/model_repository/densenet_onnx/config.pbtxt)) {
    mkdir -p $PSScriptRoot/model_repository/densenet_onnx
    wget -O $PSScriptRoot/model_repository/densenet_onnx/config.pbtxt `
        https://raw.githubusercontent.com/triton-inference-server/server/main/docs/examples/model_repository/densenet_onnx/config.pbtxt
}

if (!(Test-Path $PSScriptRoot/model_repository/densenet_onnx/densenet_labels.txt)) {
    wget -O $PSScriptRoot/model_repository/densenet_onnx/densenet_labels.txt `
        https://raw.githubusercontent.com/triton-inference-server/server/main/docs/examples/model_repository/densenet_onnx/densenet_labels.txt
}

if (!(Test-Path $PSScriptRoot/model_repository/densenet_onnx/1/model.onnx)) {
    mkdir -p $PSScriptRoot/model_repository/densenet_onnx/1
    wget -O $PSScriptRoot/model_repository/densenet_onnx/1/model.onnx `
        https://contentmamluswest001.blob.core.windows.net/content/14b2744cf8d6418c87ffddc3f3127242/9502630827244d60a1214f250e3bbca7/08aed7327d694b8dbaee2c97b8d0fcba/densenet121-1.2.onnx
}
