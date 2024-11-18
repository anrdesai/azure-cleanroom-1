import enum
import json
import os

from abc import ABCMeta, abstractmethod
from jwcrypto import jwk
from subprocess import run
from src.connectors.httpconnectors import *
from src.exceptions.custom_exceptions import (
    PrivateACRCmdExecutorFailure,
    EncContainerCmdExecutorFailure,
)
from src.logger.base_logger import logging

logger = logging.getLogger()


class SCOPE(enum.StrEnum):
    HSM_SCOPE = ("https://managedhsm.azure.net/.default",)
    MGMT_SCOPE = "https://management.azure.com/.default"


class TempFileHandler:
    def __init__(self, filename):
        self.filename = filename
        self.file = None

    def __enter__(self):
        logger.info(f"creating temp file {self.filename}")
        self.file = open(self.filename, "wb")
        return self.file

    def __exit__(self, exc_ty, exc_val, tb):
        self.file.close()
        os.remove(self.filename)
        logger.info(f"successfully deleted the temp file {self.filename}")


class BaseCmdExecutor(metaclass=ABCMeta):
    @abstractmethod
    def execute(self, args):
        pass


class PrivateACRCmdExecutor(BaseCmdExecutor):
    def execute(self, args):
        if args.private_acr_fqdn is not None:
            logger.info(f"logging into the private registry {args.private_acr_fqdn}")
            try:
                aad_token = IdentityHttpConnector.fetch_aad_token(
                    args.tenant_id,
                    args.client_id,
                    args.identity_port,
                    SCOPE.MGMT_SCOPE.value,
                )

                acr_ref_token = ACROAuthHttpConnector.fetch_acr_refresh_token(
                    args.private_acr_fqdn, args.tenant_id, aad_token
                )

                podman_login_status = run(
                    [
                        "podman",
                        "login",
                        "-u",
                        "00000000-0000-0000-0000-000000000000",
                        "-p",
                        acr_ref_token,
                        args.private_acr_fqdn,
                        "-v",
                    ]
                )

                if podman_login_status.returncode != 0:
                    raise RuntimeError("Login via podman failed.")
                logger.info(f"logged into the private registry {args.private_acr_fqdn}")
            except Exception as e:
                logger.error(
                    f"failed to login into the private registry {args.private_acr_fqdn}"
                )
                raise PrivateACRCmdExecutorFailure("failed private acr cmd") from e


class EncContainerCmdExecutor(BaseCmdExecutor):
    def execute(self, args):
        if args.encrypted_image is not None:
            logger.info(
                f"Pulling and decrypting the container image {args.encrypted_image}"
            )
            try:
                aad_token = IdentityHttpConnector.fetch_aad_token(
                    args.tenant_id,
                    args.client_id,
                    args.identity_port,
                    SCOPE.HSM_SCOPE.value,
                )
                jwt_key_str = SkrHttpConnector.get_released_jwt_key(
                    args.akv_endpoint,
                    args.maa_endpoint,
                    args.kid,
                    args.skr_port,
                    aad_token,
                )
                jwt_key_json = json.loads(jwt_key_str)

                buff_ = jwk.JWK(**jwt_key_json).export_to_pem(
                    private_key=True, password=None
                )
                tempfilehandler_ = TempFileHandler("privkey.pem")
                with tempfilehandler_ as file_:
                    file_.write(buff_)
                    file_.flush()
                    pullstatus = run(
                        [
                            "podman",
                            "pull",
                            "--decryption-key=privkey.pem",
                            args.encrypted_image,
                        ]
                    )
                    if pullstatus.returncode != 0:
                        raise RuntimeError(
                            f"podman pull failed with status {pullstatus}"
                        )
                logger.info("Successfully pulled encrypted container image")
            except Exception as e:
                logger.error(
                    f"failed to pull the encrypted image {args.encrypted_image}"
                )
                raise EncContainerCmdExecutorFailure(
                    "Failed to pull encrypted container image"
                ) from e
