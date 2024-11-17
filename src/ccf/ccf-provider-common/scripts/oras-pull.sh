#! /bin/bash

registryUrl=""
outdir=""
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

function usage()
{
    echo "Usage:"
    echo "  $0 --registryUrl <registryUrl> --out <output directory>"
    echo "Downloads the OCI artifact in the specified directory."
    echo ""
}

while [ "$1" != "" ]; do
    case $1 in
        -h|-\?|--help)
            usage
            exit 0
            ;;
        --registryUrl)
            registryUrl="$2"
            shift
            ;;
        --out)
            outdir="$2"
            shift
            ;;
        *)
            break
    esac
    shift
done

# Validate parameters
if [ -z "$registryUrl" ]; then
    echo "Error: The registryUrl parameter must be specified"
    exit 1
fi

if [ -z "$outdir" ]; then
    echo "Error: The out parameter must be specified"
    exit 1
fi

echo "oras pull $registryUrl -o $outdir"
oras pull $registryUrl -o $outdir
