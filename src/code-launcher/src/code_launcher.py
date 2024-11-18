#!/usr/bin/env python3

import argparse
import os
import sys
import shutil
import glob
import time
import socket

from subprocess import run

from src.connectors.httpconnectors import GovernanceHttpConnector
from src.cmd_executors.executors import PrivateACRCmdExecutor, EncContainerCmdExecutor
from src.logger.base_logger import initialize_logger, logging
from src.exceptions.exceptions_decorators import code_launcher_exception_decorator
from src.exceptions.custom_exceptions import (
    MountPointUnavailableFailure,
    PodmanContainerLaunchReturnedNonZeroExitCode,
    TelemetryCaptureFailure,
    ServiceNotAvailableFailure,
)


def log_args(logger: logging.Logger, args: argparse.Namespace):
    logger.info("Arguments:")
    for arg in vars(args):
        logger.info(f"{arg}: {getattr(args, arg)}")


def parse_args(cmd_args):
    arg_parser = argparse.ArgumentParser(
        description="Launch container in non-root namespace"
    )
    # aad_token params
    arg_parser.add_argument(
        "--tenant-id",
        type=str,
        default=os.environ.get("TENANT_ID"),
        help="The tenant ID for the MSI token.Goes along with --client-id.",
    )
    arg_parser.add_argument(
        "--client-id",
        type=str,
        default="",
        help="The client ID for the MSI token.Goes along with --tenant-id.",
    )
    arg_parser.add_argument(
        "--identity-port",
        type=int,
        default=8290,
        help="The port for the SKR sidecar. Defaults to 8284. Goes along with --encrypted-image.",
    )
    # private registry params
    arg_parser.add_argument(
        "--private-acr-fqdn",
        type=str,
        default=os.environ.get("PRIVATE_ACR_FQDN"),
        help="Private registry FQDN. The associated identity should have pull rbac to the registry.",
    )
    # skr params
    arg_parser.add_argument(
        "--skr-port",
        type=int,
        default=8284,
        help="The port for the SKR sidecar. Defaults to 8284. Goes along with --encrypted-image.",
    )
    arg_parser.add_argument(
        "--encrypted-image",
        type=str,
        default=os.environ.get("ENCRYPTED_IMAGE"),
        help="Image is encrypted and requires secure key release for decryption.",
    )
    arg_parser.add_argument(
        "--maa-endpoint",
        type=str,
        default=os.environ.get("MAA_ENDPOINT"),
        help="The MAA endpoint to use for secure key release. Goes along with --encrypted-image.",
    )
    arg_parser.add_argument(
        "--akv-endpoint",
        type=str,
        default=os.environ.get("KID"),
        help="The Azure Key Vault to use for secure key release.Goes along with --encrypted-image.",
    )
    arg_parser.add_argument(
        "--kid",
        type=str,
        default=os.environ.get("AKV_ENDPOINT"),
        help="The key ID (secret name) in AKV. Goes along with --encrypted-image",
    )
    # Telemetry params
    arg_parser.add_argument(
        "--export-telemetry-path",
        type=str,
        default=None,
        help="Directory for telemetry export",
    )
    arg_parser.add_argument(
        "--application-name",
        type=str,
        default="app",
        help="Application Name for telemetry export",
    )
    # governance params
    arg_parser.add_argument(
        "--governance-port",
        type=int,
        default=8300,
        help="The port for the governance sidecar. Defaults to 8300.",
    )
    # podman params
    arg_parser.add_argument(
        "podmanrunparams", nargs="*", default={}, help="arguments to launch podman"
    )
    # application mount points
    arg_parser.add_argument(
        "--wait-on-application-mounts",
        type=str,
        nargs="*",
        default=[],
        help="Application mount points to wait for readiness, before launching the application.",
    )
    # secrets params
    arg_parser.add_argument(
        "--secrets_port",
        type=int,
        default=9300,
        help="The port for the secrets sidecar. Defaults to 9300.",
    )
    # otelcollector params
    arg_parser.add_argument(
        "--otelcollector_port",
        type=int,
        default=4317,
        help="The port for the otelcollector sidecar. Defaults to 4317.",
    )
    parsed_args = arg_parser.parse_args(cmd_args)
    validate_args(parsed_args, arg_parser)
    return parsed_args


def validate_args(cmd_args, arg_parser):
    if cmd_args.private_acr_fqdn is not None and (cmd_args.tenant_id is None):
        arg_parser.error("tenant-id is required for private acr support")

    if cmd_args.encrypted_image is not None and (
        cmd_args.tenant_id is None
        or cmd_args.client_id is None
        or cmd_args.maa_endpoint is None
        or cmd_args.akv_endpoint is None
        or cmd_args.kid is None
    ):
        arg_parser.error(
            "tenan-id, client-id, maa-endpoint, akv-endpoint, and kid are required for encrypted image support"
        )

    if len(cmd_args.podmanrunparams) == 0:
        arg_parser.error("requires atleast 1 poadmanrunparams arg(s), only received 0")


@code_launcher_exception_decorator
def main(cmd_params, log_config_file, log_file_dir):
    # logging initialization
    log_file_name = os.environ.get("APPLICATION_NAME", "no-name") + "-code-launcher.log"
    initialize_logger(log_config_file, log_file_dir, log_file_name)
    logger = logging.getLogger()

    # command Parsing
    cmd_arguments = parse_args(cmd_params)
    log_args(logger, cmd_arguments)

    # execute handlers
    for handlers in [PrivateACRCmdExecutor(), EncContainerCmdExecutor()]:
        handlers.execute(cmd_arguments)

    if wait_for_services_enabled():
        # wait for critical infra services to be ready
        wait_for_services_readiness(
            [
                cmd_arguments.governance_port,
                cmd_arguments.skr_port,
                cmd_arguments.identity_port,
                cmd_arguments.secrets_port,
                cmd_arguments.otelcollector_port,
            ]
        )

    # waiting for all application mount points to be available before launching the podman cmd
    if cmd_arguments.wait_on_application_mounts:
        for mount_point in cmd_arguments.wait_on_application_mounts:
            logger.info(f"waiting for mount point {mount_point}")
            wait_for_mount_point(mount_point)

    logger.info("creating application logs directory")
    os.makedirs("/mnt/telemetry/application/", exist_ok=True)
    command = [
        "podman",
        "run",
        "--log-driver=json-file",
        f"--log-opt=path=/mnt/telemetry/application/{cmd_arguments.application_name}.log",
        "--user=1000:1000",
    ]
    command.extend(cmd_arguments.podmanrunparams)
    logger.info(f"executing podman with command: {command}")

    if events_enabled():
        GovernanceHttpConnector.put_event(
            f"starting execution of {cmd_arguments.application_name} container",
            cmd_arguments.governance_port,
        )

    result = run(command)
    logger.info(f"container exited with exit code: {result.returncode}")

    if events_enabled():
        GovernanceHttpConnector.put_event(
            f"{cmd_arguments.application_name} container terminated with exit code {result.returncode}",
            cmd_arguments.governance_port,
        )

    if cmd_arguments.export_telemetry_path is not None:
        wait_for_mount_point(
            f"{cmd_arguments.export_telemetry_path}/application-telemetry"
        )
        wait_for_mount_point(
            f"{cmd_arguments.export_telemetry_path}/infrastructure-telemetry"
        )
        logger.info(f"Copying telemetry data to {cmd_arguments.export_telemetry_path}")
        try:
            # This should ideally have been done recursively,
            # but due to a bug in blobfuse where virtual directories
            # do not work with CPK, we flatten the directory structure
            # and copy all files to the remote telemetry directory.
            # Use 'shutil.copytree("/mnt/telemetry/application", cmd_arguments.export_telemetry_path/application-telemetry)'
            for file in glob.glob("/mnt/telemetry/application/*.*"):
                if os.path.isfile(file):
                    shutil.copy(
                        file,
                        f"{cmd_arguments.export_telemetry_path}/application-telemetry/",
                    )
            # Use 'shutil.copytree("/mnt/telemetry/infrastructure", cmd_arguments.export_telemetry_path/infrastructure-telemetry)'
            for file in glob.glob("/mnt/telemetry/*.*", recursive=False):
                if os.path.isfile(file):
                    shutil.copy(
                        file,
                        f"{cmd_arguments.export_telemetry_path}/infrastructure-telemetry/",
                    )

        except Exception as e:
            raise TelemetryCaptureFailure("Failed to export logs") from e

    if result.returncode != 0:
        raise PodmanContainerLaunchReturnedNonZeroExitCode(
            result.returncode, f"Podman Returned {result.returncode} exit code"
        )


def events_enabled():
    if os.environ.get("DISABLE_GOV_EVENTS") == "true":
        return False
    return True


def wait_for_services_enabled():
    if os.environ.get("DISABLE_WAIT_FOR_SERVICES") == "true":
        return False
    return True


def wait_for_mount_point(mount_path):
    logger = logging.getLogger()
    max_retries = 12
    delay = 5
    attempt = 0
    while attempt < max_retries and (not os.path.exists(mount_path)):
        logger.info(f"Waiting for mount point {mount_path}")
        attempt += 1
        time.sleep(delay)

    if not os.path.isdir(mount_path):
        raise MountPointUnavailableFailure(f"Mount point {mount_path} is not available")

    logger.info(f"Mount point {mount_path} is available")


def wait_for_services_readiness(service_ports):
    logger = logging.getLogger()
    max_retries = 60
    delay = 5
    attempt = 0

    for service_port in service_ports:
        logger.info(f"Waiting for readines of the service on port {service_port}")
        while attempt < max_retries:
            try:
                s = socket.socket()
                s.connect(("localhost", service_port))
                logger.info(f"Service on port {service_port} is available")
                break
            except:
                logger.info(
                    f"Service on port {service_port} is not available. Retrying in {delay} seconds"
                )
                attempt += 1
                time.sleep(delay)
            finally:
                s.close()
        if attempt == max_retries:
            raise ServiceNotAvailableFailure(
                f"Service on port {service_port} is not available even after waiting for the threshold. Exiting..."
            )


if __name__ == "__main__":
    main(sys.argv[1:], "./logger/logconfig.ini", "/mnt/telemetry/")
