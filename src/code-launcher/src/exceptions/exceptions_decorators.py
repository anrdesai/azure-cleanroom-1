from functools import wraps
from src.exceptions.custom_exceptions import *
from src.logger.base_logger import logging

logger = logging.getLogger()


def code_launcher_exception_decorator(func):
    """
    Custom decorator for code_launcher exception handling
    """

    @wraps(func)
    def wrapper(*args, **kwargs):
        try:
            return func(*args, **kwargs)
        except TelemetryCaptureFailure as e:
            logger.error(
                "Copying the podman logs failed."
                " Pls check telemetry mount for podman logs"
            )
            logger.exception(e)
        except CodeLauncherExceptions as e:
            logger.exception(e)
            raise SystemExit(e.error_code)
        except Exception as e:
            logger.exception(e)
            raise e

    return wrapper
