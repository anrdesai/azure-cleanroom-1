#!/bin/bash
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the Apache 2.0 License.
# From https://raw.githubusercontent.com/microsoft/CCF/main/python/utils/keygenerator.sh

set -e

DEFAULT_CURVE="secp384r1"
FAST_CURVE="secp256r1"
SUPPORTED_CURVES="$DEFAULT_CURVE|$FAST_CURVE"

DIGEST_SHA384="sha384"
DIGEST_SHA256="sha256"

RSA_SIZE=2048

curve=$DEFAULT_CURVE
name=""
generate_encryption_key=false
output_directory=""

function usage()
{
    echo "Usage:"
    echo "  $0 --name participant_name [--curve $DEFAULT_CURVE] [--gen-enc-key] [--out output_directory]"
    echo "Generates identity private key and self-signed certificates for CCF participants."
    echo "Optionally generates a RSA key pair for recovery share encryption (optionally for consortium members only)."
    echo ""
    echo "Supported curves are: $SUPPORTED_CURVES"
}

while [ "$1" != "" ]; do
    case $1 in
        -h|-\?|--help)
            usage
            exit 0
            ;;
        -n|--name)
            name="$2"
            shift
            ;;
        -c|--curve)
            curve="$2"
            shift
            ;;
        -g|--gen-enc-key)
            generate_encryption_key=true
            ;;
        -o|--out)
            output_directory="$2"
            shift
            ;;
        *)
            break
    esac
    shift
done

# Validate parameters
if [ -z "$name" ]; then
    echo "Error: The name of the participant should be specified (e.g. member0 or user1)"
    exit 1
fi

if ! [[ "$curve" =~ ^($SUPPORTED_CURVES)$ ]]; then
    echo "$curve curve is not in $SUPPORTED_CURVES"
    exit 1
fi

if [ "$curve" == "$DEFAULT_CURVE" ]; then
    digest="$DIGEST_SHA384"
else
    digest="$DIGEST_SHA256"
fi

cert="$name"_cert.pem
privk="$name"_privk.pem
enc_priv="$name"_enc_privk.pem
enc_pub="$name"_enc_pubk.pem

if [ -n "$output_directory" ]; then
    output_directory=$(echo "$output_directory" | sed 's:/*$::')
    cert="$output_directory/$cert"
    privk="$output_directory/$privk"
    enc_priv="$output_directory/$enc_priv"
    enc_pub="$output_directory/$enc_pub"
fi

echo "-- Generating identity private key and certificate for participant \"$name\"..."
echo "Identity curve: $curve"


openssl ecparam -out "$privk" -name "$curve" -genkey
openssl req -new -key "$privk" -x509 -nodes -days 365 -out "$cert" -"$digest" -subj=/CN="$name"

echo "Identity private key generated at:   $privk"
echo "Identity certificate generated at:   $cert (to be registered in CCF)"

if "$generate_encryption_key"; then
    echo "-- Generating RSA encryption key pair for participant \"$name\"..."

    openssl genrsa -out "$enc_priv" "$RSA_SIZE"
    openssl rsa -in "$enc_priv" -pubout -out "$enc_pub"

    echo "Encryption private key generated at:  $enc_priv"
    echo "Encryption public key generated at:   $enc_pub (to be registered in CCF)"
fi