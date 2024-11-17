from azure.cli.core.commands.client_factory import get_mgmt_service_client


def cf_confidentialledger_cl(cli_ctx, *_):
    from azext_cleanroom.vendored_sdks.confidentialledger import ConfidentialLedger

    return get_mgmt_service_client(cli_ctx, ConfidentialLedger)


def cf_keyvault_cl(cli_ctx, *_):
    from azext_cleanroom.vendored_sdks.keyvault import KeyVaultManagementClient

    return get_mgmt_service_client(cli_ctx, KeyVaultManagementClient)


def cf_storage_cl(cli_ctx, *_):
    from azext_cleanroom.vendored_sdks.storage import StorageManagementClient

    return get_mgmt_service_client(cli_ctx, StorageManagementClient)


def cf_managed_ccf(cli_ctx, *_):
    return cf_confidentialledger_cl(cli_ctx).managed_ccf


def cf_key_vault(cli_ctx, *_):
    return cf_keyvault_cl(cli_ctx).vaults


def cf_storage_account(cli_ctx, *_):
    return cf_storage_cl(cli_ctx).storage_accounts


def cf_storage_container(cli_ctx, *_):
    return cf_storage_cl(cli_ctx).blob_containers
