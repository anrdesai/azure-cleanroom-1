from .models.secretstore import SecretStoreEntry, SecretStoreSpecification


def secretstore_add_cmd(
    cmd,
    secretstore_name: str,
    secretstore_config_file: str,
    backingstore_type: SecretStoreEntry.SecretStoreType,
    backingstore_id: str = "",
    backingstore_path: str = "",
    attestation_endpoint: str = "",
):
    import os
    from .utilities._azcli_helpers import logger, az_cli
    from .utilities._configuration_helpers import (
        read_secretstore_config,
        write_secretstore_config,
    )

    if os.path.exists(secretstore_config_file):
        secretstore_config = read_secretstore_config(secretstore_config_file)
    else:
        secretstore_config = SecretStoreSpecification(secretstores=[])

    for index, x in enumerate(secretstore_config.secretstores):
        if x.name == secretstore_name:
            logger.error(
                f"Secret store '{secretstore_name}' already exists ({index}):\\n{x}"
            )
            return

    if backingstore_type == SecretStoreEntry.SecretStoreType.Local_File:
        assert backingstore_path is not ""
        storeProviderUrl = backingstore_path
        configuration = ""
        supported_secret_types = [
            SecretStoreEntry.SupportedSecretTypes.Secret,
        ]
        if not os.path.exists(backingstore_path):
            os.makedirs(backingstore_path)
    else:
        assert backingstore_id is not ""
        kv_details = az_cli(f"resource show --id {backingstore_id}")
        match backingstore_type:
            case SecretStoreEntry.SecretStoreType.Azure_KeyVault_Managed_HSM:
                if kv_details["type"] == "Microsoft.KeyVault/managedHSMs":
                    storeProviderUrl = kv_details["properties"]["hsmUri"]
                else:
                    assert kv_details["type"] == "Microsoft.KeyVault/vaults"
                    assert kv_details["properties"]["sku"]["name"].lower() == "premium"
                    storeProviderUrl = kv_details["properties"]["vaultUri"]

                assert attestation_endpoint is not ""
                configuration = str({"authority": attestation_endpoint})
                supported_secret_types = [
                    SecretStoreEntry.SupportedSecretTypes.Key,
                ]
            case SecretStoreEntry.SecretStoreType.Azure_KeyVault:
                assert kv_details["type"] == "Microsoft.KeyVault/vaults"
                storeProviderUrl = kv_details["properties"]["vaultUri"]
                configuration = ""
                supported_secret_types = [
                    SecretStoreEntry.SupportedSecretTypes.Secret,
                ]

    secretstore_entry = SecretStoreEntry(
        name=secretstore_name,
        storeProviderUrl=storeProviderUrl,
        secretStoreType=backingstore_type,
        configuration=configuration,
        supportedSecretTypes=supported_secret_types,
    )

    secretstore_config.secretstores.append(secretstore_entry)

    write_secretstore_config(secretstore_config_file, secretstore_config)
    logger.warning(f"Secret store '{secretstore_name}' added to configuration.")
