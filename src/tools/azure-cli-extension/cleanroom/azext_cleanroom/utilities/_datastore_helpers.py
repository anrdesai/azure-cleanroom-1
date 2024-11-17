from ..models.datastore import *
from ..models.model import *


class Encryptor:
    import os

    aes_encryptor_so = f"{os.path.dirname(__file__)}{os.path.sep}..{os.path.sep}binaries{os.path.sep}aes_encryptor.so"

    from typing import Final

    # encryptor tooling constants
    NonceSize: Final[int] = 12
    AuthTagSize: Final[int] = 16
    PadLengthSize: Final[int] = 8
    MetaSize: Final[int] = NonceSize + AuthTagSize

    from enum import Enum

    class Operation(Enum):
        Encrypt = "Encrypt"
        Decrypt = "Decrypt"


def get_datastore(datastore_name, datastore_config_file) -> DatastoreEntry:
    from azure.cli.core.util import CLIError
    from ._configuration_helpers import read_datastore_config

    datastore_config = read_datastore_config(datastore_config_file)
    for index, x in enumerate(datastore_config.datastores):
        if x.name == datastore_name:
            datastore = datastore_config.datastores[index]
            break
    else:
        raise CLIError(
            f"Datastore {datastore_name} not found. Run az cleanroom datastore add first."
        )

    return datastore


def azcopy(
    source_location: str,
    target_location: str,
    use_cpk: bool,
    encryption_key: bytes,
):
    import os, hashlib, base64
    from azure.cli.core.util import CLIError
    from ._azcli_helpers import logger, az_cli

    # Get the tenant Id of the logged in user and indicate azcopy to use the tenant Id.
    # https://learn.microsoft.com/en-us/azure/storage/common/storage-ref-azcopy-configuration-settings
    tenant_id = az_cli("account show --query tenantId -o tsv")
    if isinstance(tenant_id, str):
        os.environ["AZCOPY_TENANT_ID"] = tenant_id

    azcopy_auto_login_type = "AZCLI"
    account_details = az_cli("account show")
    if "user" in account_details:
        if (
            account_details["user"]["name"] == "userAssignedIdentity"
            or account_details["user"]["name"] == "systemAssignedIdentity"
        ):
            azcopy_auto_login_type = "MSI"

    os.environ["AZCOPY_AUTO_LOGIN_TYPE"] = azcopy_auto_login_type
    azcopy_cmd = [
        "azcopy",
        "copy",
        source_location,
        target_location,
        "--recursive",
    ]

    if use_cpk:
        # azcopy with CPK needs the values below for encryption
        # https://learn.microsoft.com/en-us/azure/storage/common/storage-ref-azcopy-copy
        encryption_key_base_64 = base64.b64encode(encryption_key).decode("utf-8")
        encryption_key_sha256 = hashlib.sha256(encryption_key).digest()
        encryption_key_sha256_base_64 = base64.b64encode(encryption_key_sha256).decode(
            "utf-8"
        )
        os.environ["CPK_ENCRYPTION_KEY"] = encryption_key_base_64
        os.environ["CPK_ENCRYPTION_KEY_SHA256"] = encryption_key_sha256_base_64

        azcopy_cmd.append("--cpk-by-value")

    import subprocess

    result: subprocess.CompletedProcess
    try:
        logger.warning(f"Copying dataset from {source_location} to {target_location}")

        result = subprocess.run(
            azcopy_cmd,
            capture_output=True,
        )
    except FileNotFoundError:
        raise CLIError(
            "azcopy not installed. Install from https://github.com/Azure/azure-storage-azcopy?tab=readme-ov-file#download-azcopy and try again."
        )

    try:
        for line in str.splitlines(result.stdout.decode()):
            logger.warning(line)
        for line in str.splitlines(result.stderr.decode()):
            logger.warning(line)
        result.check_returncode()
    except subprocess.CalledProcessError:
        for line in str.splitlines(result.stdout.decode()):
            logger.error(line)
        for line in str.splitlines(result.stderr.decode()):
            logger.error(line)
        raise CLIError("Failed to copy data. See error details above.")


def config_add_datastore(
    cleanroom_config_file,
    datastore_name,
    datastore_config_file,
    identity,
    secretstore_config_file,
    dek_secret_store,
    kek_secret_store,
    kek_name,
    access_mode,
    access_name="",
):
    from azure.cli.core.util import CLIError
    from ._azcli_helpers import logger
    from ._configuration_helpers import (
        read_cleanroom_spec,
        write_cleanroom_spec,
    )
    from ._secretstore_helpers import get_secretstore_entry

    access_name = access_name or datastore_name
    cleanroom_spec = read_cleanroom_spec(cleanroom_config_file)

    access_identity = [x for x in cleanroom_spec.identities if x.name == identity]
    if len(access_identity) == 0:
        raise CLIError("Run az cleanroom config add-identity first.")
    import uuid

    datastore_entry = get_datastore(datastore_name, datastore_config_file)

    kek_name = kek_name or (
        str(uuid.uuid3(uuid.NAMESPACE_X500, cleanroom_config_file))[:8] + "-kek"
    )
    wrapped_dek_name = f"wrapped-{datastore_name}-dek-{kek_name}"
    if datastore_entry.storeType == DatastoreEntry.StoreType.Azure_BlobStorage:
        proxy_type = (
            ProxyType.SecureVolume__ReadOnly__Azure__BlobStorage
            if access_mode == DatastoreEntry.AccessMode.Source
            else ProxyType.SecureVolume__ReadWrite__Azure__BlobStorage
        )
        provider_protocol = ProtocolType.Azure_BlobStorage
    elif datastore_entry.storeType == DatastoreEntry.StoreType.Azure_OneLake:
        proxy_type = (
            ProxyType.SecureVolume__ReadOnly__Azure__OneLake
            if access_mode == DatastoreEntry.AccessMode.Source
            else ProxyType.SecureVolume__ReadWrite__Azure__OneLake
        )
        provider_protocol = ProtocolType.Azure_OneLake

    dek_secret_store_entry = get_secretstore_entry(
        dek_secret_store, secretstore_config_file
    )
    kek_secret_store_entry = get_secretstore_entry(
        kek_secret_store, secretstore_config_file
    )

    # TODO (HPrabh): Remove this check when key release is supported directly on the DEK.
    if not dek_secret_store_entry.is_secret_supported():
        raise CLIError(
            "Unsupported secret store for DEK. Please use Standard or Premium Key Vault"
        )

    if not kek_secret_store_entry.is_key_release_supported():
        raise CLIError(
            "Unsupported secret store for KEK. Please use MHSM or Premium Key Vault"
        )

    encryption_mode = str(datastore_entry.encryptionMode)

    store = Resource(
        name=datastore_entry.storeName,
        type=ResourceType(str(datastore_entry.storeType)),
        id=datastore_name,
        provider=ServiceEndpoint(
            protocol=provider_protocol, url=datastore_entry.storeProviderUrl
        ),
    )

    privacyProxySettings = PrivacyProxySettings(
        proxyType=proxy_type,
        proxyMode=ProxyMode.Secure,
        configuration=str({"KeyType": "KEK", "EncryptionMode": encryption_mode}),
        encryptionSecrets=EncryptionSecrets(
            # TODO (HPrabh): Add support for DEK to be key released without having a wrapping KEK.
            dek=EncryptionSecret(
                name=wrapped_dek_name,
                secret=CleanroomSecret(
                    secretType=SecretType.Key,
                    backingResource=Resource(
                        id=dek_secret_store,
                        name=wrapped_dek_name,
                        type=ResourceType.AzureKeyVault,
                        provider=ServiceEndpoint(
                            protocol=ProtocolType.AzureKeyVault_Secret,
                            url=dek_secret_store_entry.storeProviderUrl,
                        ),
                    ),
                ),
            ),
            kek=EncryptionSecret(
                name=kek_name,
                secret=CleanroomSecret(
                    secretType=SecretType.Key,
                    backingResource=Resource(
                        id=kek_secret_store,
                        name=kek_name,
                        type=ResourceType.AzureKeyVault,
                        provider=ServiceEndpoint(
                            protocol=ProtocolType.AzureKeyVault_SecureKey,
                            url=kek_secret_store_entry.storeProviderUrl,
                            configuration=kek_secret_store_entry.configuration,
                        ),
                    ),
                ),
            ),
        ),
        encryptionSecretAccessIdentity=access_identity[0],
    )

    if access_mode == DatastoreEntry.AccessMode.Source:
        node = "datasources"
        candidate_list = cleanroom_spec.datasources
        access_point_type = AccessPointType.Volume_ReadOnly
    else:
        assert access_mode == DatastoreEntry.AccessMode.Sink
        node = "datasinks"
        candidate_list = cleanroom_spec.datasinks
        access_point_type = AccessPointType.Volume_ReadWrite

    access_point = AccessPoint(
        name=access_name,
        type=access_point_type,
        path="",
        store=store,
        identity=access_identity[0],
        protection=privacyProxySettings,
    )

    index = next(
        (i for i, x in enumerate(candidate_list) if x.name == access_point.name),
        None,
    )
    if index == None:
        logger.info(f"Adding entry for {node} {access_point.name} in configuration.")
        candidate_list.append(access_point)
    else:
        logger.info(f"Patching {node} {access_point.name} in configuration.")
        candidate_list[index] = access_point

    write_cleanroom_spec(cleanroom_config_file, cleanroom_spec)
    logger.warning(f"'{datastore_name}' added to '{node}' in cleanroom configuration.")


def config_get_datastore_name(cleanroom_config, access_name, access_mode):
    from azure.cli.core.util import CLIError
    from ..datastore_cmd import DatastoreEntry
    from ._configuration_helpers import read_cleanroom_spec

    spec = read_cleanroom_spec(cleanroom_config)
    eligible_candidates = (
        spec.datasources
        if access_mode == DatastoreEntry.AccessMode.Source
        else spec.datasinks
    )
    candidates = [x for x in eligible_candidates if x.name == access_name]
    if len(candidates) == 0:
        raise CLIError(f"{access_name} not found in cleanroom configuration.")

    datastore_name = candidates[0].store.id
    return datastore_name


def generate_safe_datastore_name(prefix, unique_name, friendly_name):
    import uuid

    safe_name = (
        f"{prefix}-"
        + str(uuid.uuid3(uuid.NAMESPACE_X500, unique_name))[:8]
        + f"-{friendly_name}"
    )[:63]

    return safe_name


def cryptocopy(
    operation: Encryptor.Operation,
    datastore_name,
    datastore_config_file,
    source_path,
    destination_path,
    blockSize,
):
    import os, glob, base64
    from ._azcli_helpers import logger
    from ._datastore_helpers import get_datastore
    from ._secretstore_helpers import get_secretstore, get_secretstore_entry

    datastore = get_datastore(datastore_name, datastore_config_file)
    if operation == Encryptor.Operation.Decrypt:
        source_path = os.path.join(source_path, datastore.name, datastore.storeName)
        destination_path = os.path.join(
            destination_path, datastore.name, datastore.storeName
        )

    os.makedirs(destination_path, mode=0o755, exist_ok=True)
    # Get the key path.
    secret_store = get_secretstore(
        get_secretstore_entry(datastore.secretstore_name, datastore.secretstore_config)
    )
    encryption_key = secret_store.get_secret(datastore_name)
    blockSize = int(blockSize)
    blockSize *= 1024 * 1024

    # Recursively encypt/decrypt files in the source path.
    for source_file in glob.glob(os.path.join(source_path, "**/*"), recursive=True):
        if os.path.isfile(source_file):
            source_rel_path = os.path.relpath(source_file, start=source_path)
            destination_file = os.path.join(destination_path, source_rel_path)
            destination_dir = os.path.dirname(destination_file)
            os.makedirs(destination_dir, exist_ok=True)

            logger.info(f"[{operation}] '{source_file}' -> '{destination_file}'")
            if operation == Encryptor.Operation.Encrypt:
                encrypt_file(source_file, encryption_key, blockSize, destination_file)
            else:
                decrypt_file(source_file, encryption_key, blockSize, destination_file)


def encrypt_file(plaintextPath, key, blockSize, ciphertextPath):
    import ctypes, base64, json
    from azure.cli.core.util import CLIError
    from ._azcli_helpers import logger

    try:
        with open(ciphertextPath, "wb") as ciphertextFile:
            paddingLength = 0
            with open(plaintextPath, "rb") as plaintextFile:
                while True:
                    buffer = plaintextFile.read(blockSize)
                    dataLen = len(buffer)
                    if not buffer:
                        break

                    # Padding zeros to make it multiple of blocksize.
                    if dataLen < blockSize:
                        paddingLength = blockSize - len(buffer)
                        padding = bytes(paddingLength)
                        buffer = buffer + padding

                    # Invoking go encryption code.
                    aes_encryption_lib = ctypes.CDLL(Encryptor.aes_encryptor_so)
                    encrypt = aes_encryption_lib.GoEncryptChunk
                    encrypt.argtypes = [ctypes.c_char_p]
                    encrypt.restype = ctypes.c_void_p

                    document = {
                        "Data": base64.b64encode(buffer).decode(),
                        "Key": base64.b64encode(key).decode(),
                    }
                    response = encrypt(json.dumps(document).encode("utf-8"))
                    response_bytes = ctypes.string_at(response)
                    response_string = response_bytes.decode("utf-8")
                    jsonResponse = json.loads(response_string)
                    encryptedChunk = base64.b64decode(jsonResponse.get("CipherText"))
                    nonce = base64.b64decode(jsonResponse.get("Nonce"))

                    encryptedChunk = nonce + encryptedChunk
                    n = ciphertextFile.write(encryptedChunk)
                    logger.info(
                        f"Encrypted chunk written along with nonce and auth tag, total bytes: {n}"
                    )
                paddingLengthByte = paddingLength.to_bytes(8, byteorder="big")
                n = ciphertextFile.write(paddingLengthByte)
                logger.info(f"Padding length written, total bytes: {n}")
    except Exception as e:
        raise CLIError(f"Error during encryption: {e}")


def decrypt_file(ciphertextPath, key, blockSize, plaintextPath):
    import os, ctypes, base64, json
    from azure.cli.core.util import CLIError
    from ._azcli_helpers import logger

    try:
        with open(ciphertextPath, "rb") as ciphertextFile:
            with open(plaintextPath, "wb") as plaintextFile:
                ciphertextFileSize = os.path.getsize(ciphertextPath)
                totalBlocks = (ciphertextFileSize - Encryptor.PadLengthSize) // (
                    blockSize + Encryptor.MetaSize
                )

                for i in range(totalBlocks):
                    buffer = ciphertextFile.read(blockSize + Encryptor.MetaSize)
                    nonce = buffer[: Encryptor.NonceSize]
                    buffer = buffer[Encryptor.NonceSize :]
                    if not buffer:
                        break
                    # Invoking go decryption code.
                    aes_encryption_lib = ctypes.CDLL(Encryptor.aes_encryptor_so)
                    decrypt = aes_encryption_lib.GoDecryptChunk
                    decrypt.argtypes = [ctypes.c_char_p]
                    decrypt.restype = ctypes.c_void_p

                    document = {
                        "CipherText": base64.b64encode(buffer).decode(),
                        "Nonce": base64.b64encode(nonce).decode(),
                        "Key": base64.b64encode(key).decode(),
                    }

                    response = decrypt(json.dumps(document).encode("utf-8"))
                    response_bytes = ctypes.string_at(response)
                    response_string = response_bytes.decode("utf-8")
                    jsonResponse = json.loads(response_string)
                    decryptedChunk = base64.b64decode(jsonResponse.get("PlainText"))
                    if i == totalBlocks - 1:
                        paddingLength = ciphertextFile.read(Encryptor.PadLengthSize)
                        paddingLength = int.from_bytes(paddingLength, byteorder="big")
                        decryptedChunk = decryptedChunk[
                            : len(decryptedChunk) - paddingLength
                        ]

                    n = plaintextFile.write(decryptedChunk)

    except Exception as e:
        raise CLIError(f"Error during decryption: {e}")


def generate_wrapped_dek(datastore_name, datastore_config_file, public_key):
    import base64
    from cryptography.hazmat.primitives.asymmetric import padding
    from cryptography.hazmat.primitives import hashes
    from ._datastore_helpers import get_datastore
    from ._secretstore_helpers import get_secretstore, get_secretstore_entry

    datastore = get_datastore(datastore_name, datastore_config_file)
    secret_store = get_secretstore(
        get_secretstore_entry(datastore.secretstore_name, datastore.secretstore_config)
    )
    dek_bytes = secret_store.get_secret(datastore_name)
    return base64.b64encode(
        public_key.encrypt(
            dek_bytes,
            padding.OAEP(
                mgf=padding.MGF1(algorithm=hashes.SHA256()),
                algorithm=hashes.SHA256(),
                label=None,
            ),
        )
    ).decode()
