#! /bin/bash

set -e

if [ -z "$CONFIG_DATA_TGZ" ]; then
    echo "Error: CONFIG_DATA_TGZ environment variable must be set"
    exit 1
fi

CONFIG_EXTRACT_DIR=${CONFIG_EXTRACT_DIR:-"/etc/nginx"} 

echo "Expanding config payload into $CONFIG_EXTRACT_DIR"
echo "$CONFIG_DATA_TGZ" | base64 -d | tar xz -C $CONFIG_EXTRACT_DIR

echo "Launching nginx"
exec nginx -g "daemon off;"