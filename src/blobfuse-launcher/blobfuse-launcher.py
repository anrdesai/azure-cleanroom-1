import argparse
import base64
import hashlib
import logging
import os
import uuid

from opentelemetry import _logs
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.exporter.otlp.proto.grpc._log_exporter import (
    OTLPLogExporter,
)
from opentelemetry import trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.trace.export import BatchSpanProcessor

from opentelemetry.instrumentation.requests import RequestsInstrumentor
from opentelemetry.instrumentation.logging import LoggingInstrumentor

import utilities


container_name = os.environ.get("AZURE_STORAGE_ACCOUNT_CONTAINER")
access_name = os.environ.get("ACCESS_NAME")

if container_name is None:
    raise ValueError("AZURE_STORAGE_ACCOUNT_CONTAINER environment variable is not set")

logger_name = "-".join([access_name, container_name, str(uuid.uuid4())[:8]])

# Initialize tracing.
provider = TracerProvider(
    resource=Resource.create(
        {
            "service.name": f"{logger_name}-blobfuse-launcher",
        }
    ),
)
provider.add_span_processor(BatchSpanProcessor(OTLPSpanExporter()))

# Sets the global default tracer provider.
trace.set_tracer_provider(provider)
# You can optionally pass a custom TracerProvider to instrument().
RequestsInstrumentor().instrument(tracer_provider=provider)

# Creates a tracer from the global tracer provider.
tracer = trace.get_tracer("blobfuse-launcher")

# Initialize logging
logger_provider = LoggerProvider(
    resource=Resource.create(
        {
            "service.name": f"{logger_name}-blobfuse-launcher",
        }
    ),
)
logger_provider.add_log_record_processor(
    BatchLogRecordProcessor(OTLPLogExporter(insecure=True))
)
_logs.set_logger_provider(logger_provider)
LoggingInstrumentor().instrument(set_logging_format=True)

# Create a logger from the global logger provider.
logging.basicConfig(level=logging.INFO)
handler = LoggingHandler(level=logging.NOTSET, logger_provider=logger_provider)
logger = logging.getLogger("blobfuse-launcher")
logger.addHandler(handler)


def log_args(logger: logging.Logger, args: argparse.Namespace):
    logger.info("Arguments:")
    for arg in vars(args):
        logger.info(f"{arg}: {getattr(args, arg)}")


def parse_args():
    parser = argparse.ArgumentParser(
        prog="blobfuse-launcher.py",
        description="Launch blobfuse with secure key release",
    )
    parser.add_argument(
        "--skr-port", type=int, default=8284, help="The port for the SKR sidecar"
    )
    parser.add_argument(
        "--secrets-port",
        type=int,
        default=9300,
        help="The port for the secrets sidecar",
    )
    parser.add_argument(
        "--maa-endpoint",
        type=str,
        default=os.environ.get("MAA_ENDPOINT"),
        help="The MAA endpoint to use for secure key release",
    )
    parser.add_argument(
        "--akv-endpoint",
        type=str,
        default=os.environ.get("AKV_ENDPOINT"),
        help="The Azure Key Vault to use for secure key release",
    )
    parser.add_argument(
        "--kid", type=str, default=os.environ.get("KID"), help="The key ID in AKV"
    )
    parser.add_argument(
        "--imds-port", type=int, default=8290, help="The port for the Identity sidecar"
    )
    parser.add_argument(
        "--otel-collector-port",
        type=int,
        default=4317,
        help="The port for the OTel collector",
    )
    parser.add_argument(
        "--tenant-id",
        type=str,
        default=os.environ.get("TENANT_ID"),
        help="The tenant ID for the MSI token",
    )
    parser.add_argument(
        "--client-id",
        type=str,
        default=os.environ.get("CLIENT_ID"),
        help="The client ID for the MSI token",
    )
    parser.add_argument(
        "--mount-path",
        type=str,
        default="/mnt/blob",
        help="The mount path for blobfuse",
    )
    parser.add_argument(
        "--read-only",
        type=bool,
        action=argparse.BooleanOptionalAction,
        help="The mount container in read only or not",
    )
    parser.add_argument(
        "--use-adls",
        type=bool,
        action=argparse.BooleanOptionalAction,
        help="Use ADLS as the storage backend",
    )
    parser.add_argument(
        "--wrapped-dek-secret",
        type=str,
        default=os.environ.get("WRAPPED_DEK_SECRET"),
        help="The wrapped DEK secret",
    )
    parser.add_argument(
        "--wrapped-dek-akv-endpoint",
        type=str,
        default=os.environ.get("WRAPPED_DEK_AKV_ENDPOINT"),
        help="The Azure Key Vault endpoint holding the wrapped DEK",
    )
    parser.add_argument(
        "--sub-directory",
        type=str,
        default="",
        help="The sub-directory to mount the container for onelake storage",
    )
    parser.add_argument(
        "--custom-encryption-mode",
        type=str,
        choices=["CPK", "CSE", "None"],
        default="CPK",
        help="The encryption mode to use for blobfuse",
    )

    return parser.parse_args()


@tracer.start_as_current_span("blobfuse-launcher")
def main():
    args = parse_args()
    log_args(logger, args)

    # Wait for SKR sidecar to be available as the secrets sidecar will invoke it.
    utilities.wait_for_services_readiness(
        logger,
        tracer,
        [args.otel_collector_port, args.imds_port, args.skr_port, args.secrets_port],
    )

    logger.info(
        f"Releasing key '{args.kid}' from Key vault '{args.akv_endpoint}' using MAA '{args.maa_endpoint}'"
    )

    encryption_key = utilities.unwrap_secret(
        logger,
        tracer,
        args.secrets_port,
        args.client_id,
        args.tenant_id,
        args.wrapped_dek_secret,
        args.wrapped_dek_akv_endpoint,
        args.kid,
        args.akv_endpoint,
        args.maa_endpoint,
    )
    # Create directories if they don't exist.
    os.makedirs(args.mount_path, exist_ok=True)
    os.makedirs("/tmp/blobfuse_tmp", exist_ok=True)
    os.makedirs("/mnt/telemetry/infrastructure/logs", exist_ok=True)

    encryption_key_base64 = base64.standard_b64encode(encryption_key).decode()
    os.environ["AZURE_STORAGE_AUTH_TYPE"] = "msi"
    os.environ["MSI_ENDPOINT"] = (
        f"http://localhost:{args.imds_port}/metadata/identity/{args.tenant_id}/{args.client_id}/oauth2/token"
    )

    logger.info(
        f"Starting blobfuse mount at '{args.mount_path}',"
        + f"Read Only: '{args.read_only}',"
        + f"encryption mode: '{args.custom_encryption_mode}'"
    )
    if args.custom_encryption_mode == "CPK":
        # Hash the byte array
        sha256_hash = hashlib.sha256(encryption_key).digest()
        encryption_key_sha256 = base64.b64encode(sha256_hash).decode("utf-8")
        os.environ["AZURE_STORAGE_CPK_ENCRYPTION_KEY"] = encryption_key_base64
        os.environ["AZURE_STORAGE_CPK_ENCRYPTION_KEY_SHA256"] = encryption_key_sha256

        utilities.launch_blobfuse(
            logger,
            tracer,
            args.mount_path,
            args.read_only,
            args.sub_directory,
            args.use_adls,
            True,
        )
    elif args.custom_encryption_mode == "None":
        utilities.launch_blobfuse(
            logger,
            tracer,
            args.mount_path,
            args.read_only,
            args.sub_directory,
            args.use_adls,
            False,
        )
    else:
        os.environ["ENCRYPTION_KEY"] = encryption_key_base64
        utilities.launch_blobfuse_encrypted(
            logger,
            tracer,
            args.mount_path,
            args.read_only,
        )

    # TODO (HPrabh): Handle SIGTERM.
    os.system("sleep infinity")


if __name__ == "__main__":
    main()
