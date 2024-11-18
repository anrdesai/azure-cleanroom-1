from enum import IntEnum


class SystemExitCode(IntEnum):
    ServiceNotAvailableFailureExitCode = 1001
    PrivateACRCmdExecutorFailureExitCode = 1002
    EncContainerCmdExecutorFailure = 1003
    TelemetryCaptureFailure = 1004
    MountPointUnavailableFailure = 1005


class CodeLauncherExceptions(RuntimeError):
    def __init__(self, error_code, *args: object) -> None:
        super().__init__(*args)
        self.error_code = error_code


class ServiceNotAvailableFailure(CodeLauncherExceptions):
    def __init__(self, *args: object) -> None:
        super().__init__(SystemExitCode.ServiceNotAvailableFailureExitCode.value, *args)


class PrivateACRCmdExecutorFailure(CodeLauncherExceptions):
    def __init__(self, *args: object) -> None:
        super().__init__(
            SystemExitCode.PrivateACRCmdExecutorFailureExitCode.value, *args
        )


class EncContainerCmdExecutorFailure(CodeLauncherExceptions):
    def __init__(self, *args: object) -> None:
        super().__init__(SystemExitCode.EncContainerCmdExecutorFailure.value, *args)


class PodmanContainerLaunchReturnedNonZeroExitCode(CodeLauncherExceptions):
    def __init__(self, podman_cmd_exit_code, *args: object) -> None:
        super().__init__(podman_cmd_exit_code, *args)


class TelemetryCaptureFailure(CodeLauncherExceptions):
    def __init__(self, *args: object) -> None:
        super().__init__(SystemExitCode.TelemetryCaptureFailure.value, *args)


class MountPointUnavailableFailure(CodeLauncherExceptions):
    def __init__(self, *args: object) -> None:
        super().__init__(SystemExitCode.MountPointUnavailableFailure.value, *args)
