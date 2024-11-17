#! /bin/bash
set -e

if [ -z "$CCR_ENVOY_DESTINATION_PORT" ]; then
    echo "Error: CCR_ENVOY_DESTINATION_PORT environment variable must be set"
    exit 1
fi

CCR_ENVOY_CERT_SUBJECT_NAME=${CCR_ENVOY_CERT_SUBJECT_NAME:-"/CN=CCR CA"}

# Generate self-signed certificate
BASEDIR=$(dirname "$0")
mkdir -p keys
mkdir -p certs
out_key="keys/server-key.pem"
out_cert="certs/server-cert.pem"

echo "Generating SSL certificate with subj $CCR_ENVOY_CERT_SUBJECT_NAME"
# Create CA cert
## 1. Create the root key
openssl genrsa -out keys/CA.key 2048
## 2. Create a Root Certificate and self-sign it
openssl req -x509 -new -nodes -key keys/CA.key -sha256 -days 100 -out certs/CA.crt -config $BASEDIR/cert-config.cnf -extensions v3_ca -subj "$CCR_ENVOY_CERT_SUBJECT_NAME"

# Create the node cert
## 1. Create the certificate's key
openssl genrsa -out "$out_key" 2048
 
## 2. Create the CSR (Certificate Signing Request)
openssl req -new -key "$out_key" -out cert-config.csr -config $BASEDIR/cert-config.cnf -extensions v3_req
 
## 3. Generate the certificate with the CSR and the key and sign it with the CA's root key
openssl x509 -req -in cert-config.csr -CA certs/CA.crt -CAkey keys/CA.key -CAcreateserial -out "$out_cert" -days 100 -sha256 -extfile $BASEDIR/cert-config.cnf -extensions v3_req
 
# Display the created cert
openssl x509 -in "$out_cert" -text -noout

if [ -n "$CCR_ENVOY_SERVICE_CERT_OUTPUT_FILE" ]; then
    cat certs/CA.crt > $CCR_ENVOY_SERVICE_CERT_OUTPUT_FILE
fi
export CCR_ENVOY_LISTENER_HTTPS_ENDPOINT=${CCR_ENVOY_LISTENER_HTTPS_ENDPOINT:-"0.0.0.0"}
export CCR_ENVOY_LISTENER_HTTPS_PORT=${CCR_ENVOY_LISTENER_HTTPS_PORT:-443}
export CCR_ENVOY_CLUSTER_TYPE=${CCR_ENVOY_CLUSTER_TYPE:-"STATIC"}
export CCR_ENVOY_DESTINATION_ENDPOINT=${CCR_ENVOY_DESTINATION_ENDPOINT:-"0.0.0.0"}

cat https-http/ccr-https-http-proxy-config.yaml | envsubst \
    '$CCR_ENVOY_LISTENER_HTTPS_ENDPOINT $CCR_ENVOY_LISTENER_HTTPS_PORT $CCR_ENVOY_CLUSTER_TYPE $CCR_ENVOY_DESTINATION_ENDPOINT $CCR_ENVOY_DESTINATION_PORT' \
    > /tmp/ccr-https-http-proxy-config.yaml

echo "Launching envoy"
envoy -c /tmp/ccr-https-http-proxy-config.yaml