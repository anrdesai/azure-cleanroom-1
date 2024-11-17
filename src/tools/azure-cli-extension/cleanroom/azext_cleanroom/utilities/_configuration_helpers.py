from ..models.datastore import *
from ..models.model import *
from ..models.secretstore import *
from typing import Any
from ..utilities._azcli_helpers import logger

# TODO (HPrabh): Model a Configuration class that wraps the below methods.


def read_cleanroom_spec(config_file) -> CleanRoomSpecification:
    spec = _read_configuration_file(config_file)
    return CleanRoomSpecification(**spec)


def read_datastore_config(config_file) -> DatastoreSpecification:
    spec = _read_configuration_file(config_file)
    return DatastoreSpecification(**spec)


def read_secretstore_config(config_file) -> SecretStoreSpecification:
    spec = _read_configuration_file(config_file)
    return SecretStoreSpecification(**spec)


def _read_configuration_file(config_file) -> dict[str, Any]:
    from azure.cli.core.util import CLIError
    import yaml

    logger.info(f"Reading configuration file {config_file}")
    try:
        with open(config_file, "r") as f:
            return yaml.safe_load(f)
    except FileNotFoundError:
        raise CLIError(
            f"Cannot find file {config_file}. Check the --*-config parameter value."
        )


def write_cleanroom_spec(config_file, config: CleanRoomSpecification):
    _write_configuration_file(config_file, config)


def write_datastore_config(config_file, datastore: DatastoreSpecification):
    _write_configuration_file(config_file, datastore)


def write_secretstore_config(config_file, secretstore: SecretStoreSpecification):
    _write_configuration_file(config_file, secretstore)


def _write_configuration_file(config_file, config: BaseModel):
    import yaml
    from rich import print

    print(f"Writing {config_file}")
    print(config)
    with open(config_file, "w") as f:
        yaml.dump(config.model_dump(mode="json"), f)


def generate_backcompat_datastore_configuration(cleanroom_config):
    import os

    file_name, file_ext = os.path.splitext(cleanroom_config)
    datastore_config_file = file_name + "_datastore_config" + file_ext

    datastore_keys_dir = f"{file_name}_datastore.keys"
    return datastore_config_file, datastore_keys_dir
