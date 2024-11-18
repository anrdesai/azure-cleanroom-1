#! /bin/bash

privk=""
outfile=""
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

function usage()
{
    echo "Usage:"
    echo "  $0 --privk pemfile --out pemfile"
    echo "Generates a CA certificate using the corresponding private key."
    echo ""
}

while [ "$1" != "" ]; do
    case $1 in
        -h|-\?|--help)
            usage
            exit 0
            ;;
        -p|--privk)
            privk="$2"
            shift
            ;;
        -o|--out)
            outfile="$2"
            shift
            ;;
        *)
            break
    esac
    shift
done

# Validate parameters
if [ -z "$privk" ]; then
    echo "Error: The privk parameter must be specified"
    exit 1
fi

if [ -z "$outfile" ]; then
    echo "Error: The outfile parameter must be specified"
    exit 1
fi

echo "Generating CA cert"

## Create a Root Certificate and self-sign it
openssl req -x509 -new -nodes -key $privk -sha256 -days 100 -out $outfile -config $SCRIPT_DIR/cleanroom.cnf -extensions v3_ca -subj "/CN=Azure Clean Room CA"
 