#!/bin/bash

# Uploading the blobfuse2 binary as an artifact and downloading it does not preserve the file
# permissions so the binary is not marked as executable. Hence launching it in the container fails
# with permission denied. Need to run this script after download artifact step to fix the
# permissions.
chmod +x blobfuse2-binaries/blobfuse2