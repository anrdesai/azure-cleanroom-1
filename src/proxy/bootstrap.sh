#! /bin/bash

# Generate self-signed certificate
mkdir keys
mkdir certs
out_key="keys/server-key.pem"
out_cert="certs/server-cert.pem"

if [ "$NO_CGS_CA" = "true" ]; then
    echo "Generating SSL certificate using local CA"
    # Create clean room CA cert
    ## 1. Create the root key
    openssl genrsa -out keys/CA.key 2048
    ## 2. Create a Root Certificate and self-sign it
    openssl req -x509 -new -nodes -key keys/CA.key -sha256 -days 100 -out certs/CA.crt -config ./cleanroom.cnf -extensions v3_ca -subj "/CN=Azure Clean Room CA"
else
    echo "Generating SSL certificate using CGS CA"
    GOVERNANCE_PORT=${GOVERNANCE_PORT:-8300}
    ./wait-for-it.sh --timeout=100  --strict 127.0.0.1:${GOVERNANCE_PORT} -- echo "Governance sidecar available"
    ## 1. TODO (gsinha): Fetch the CA root key as a stop gap to generate the server cert till the time CCF supports cert generation.
    curl -X POST "127.0.0.1:${GOVERNANCE_PORT}/ca/releaseSigningKey" -k --silent > ./rootkey.json
    cat ./rootkey.json | jq -r .privateKey > keys/CA.key
    cat ./rootkey.json | jq -r .caCert > certs/CA.crt
fi

# Create clean room cert
## 1. Create the certificate's key
openssl genrsa -out "$out_key" 2048
 
## 2. Create the CSR (Certificate Signing Request)
openssl req -new -key "$out_key" -out cleanroom.csr -config cleanroom.cnf -extensions v3_req
 
## 3. Generate the certificate with the CSR and the key and sign it with the CA's root key
openssl x509 -req -in cleanroom.csr -CA certs/CA.crt -CAkey keys/CA.key -CAcreateserial -out "$out_cert" -days 100 -sha256 -extfile cleanroom.cnf -extensions v3_req
 
# Display the created cert
openssl x509 -in "$out_cert" -text -noout

export CCR_SIDECAR_PORT=${CCR_SIDECAR_PORT:-8281} 
cat ccr-proxy-config.yaml | envsubst '$CCR_SIDECAR_PORT' > /tmp/ccr-proxy-config.yaml

echo "Launching envoy"
envoy -c /tmp/ccr-proxy-config.yaml