from tarfile import data_filter
import requests
import socket
import time

from src.exceptions.custom_exceptions import ServiceNotAvailableFailure
from src.logger.base_logger import logging

logger = logging.getLogger()


class HttpConnector:
    @staticmethod
    def httpget(url, query_params, headers):
        resp = requests.get(url=url, headers=headers, params=query_params)
        resp.raise_for_status()
        return resp.json()

    @staticmethod
    def httppost(
        url, query_params=None, post_params=None, json_data=None, headers=None
    ):
        resp = requests.post(
            url=url,
            params=query_params,
            data=post_params,
            json=json_data,
            headers=headers,
        )
        resp.raise_for_status()
        return resp.json()

    @staticmethod
    def httpput(url, query_params=None, data=None, json_data=None, headers=None):
        resp = requests.put(
            url=url,
            params=query_params,
            data=data,
            json=json_data,
            headers=headers,
        )
        resp.raise_for_status()
        return resp

    @staticmethod
    def wait_for_it(host, port, timeout=60, interval=5):
        endtime_ = time.time() + timeout
        socket_ = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        retrycount = 1
        while True:
            try:
                socket_.connect((host, int(port)))
                socket_.close()
                break
            except Exception as e:
                if time.time() >= endtime_:
                    socket_.close()
                    raise ServiceNotAvailableFailure(
                        f"{host} is not listening on port {port}"
                    ) from e
                else:
                    logger.error(
                        f"connecting to {host} on port {port} failed times={retrycount}, exception={e.args}"
                    )
                    retrycount = retrycount + 1
                    time.sleep(interval)
                    continue


class IdentityHttpConnector:
    @staticmethod
    def fetch_aad_token(tenant_id, client_id, identity_port, scope):
        HttpConnector.wait_for_it("localhost", identity_port)
        url = f"http://localhost:{identity_port}/metadata/identity/oauth2/token"
        query_params = {
            "scope": scope,
            "tenantId": tenant_id,
            "clientId": client_id,
            "apiVersion": "2018-02-01",
        }
        headers = {"Content-Type": "application/json"}
        resp = HttpConnector.httpget(url, query_params, headers)
        if resp.get("token") is None:
            raise RuntimeError("arc refresh_token not found in response")
        return resp.get("token")


class ACROAuthHttpConnector:
    @staticmethod
    def fetch_acr_refresh_token(private_acr_fqdn, tenant_id, aad_token):
        url = f"https://{private_acr_fqdn}/oauth2/exchange"
        post_params = f"grant_type=access_token&service={private_acr_fqdn}&tenant={tenant_id}&access_token={aad_token}"
        headers = {"Content-Type": "application/x-www-form-urlencoded"}
        resp = HttpConnector.httppost(url=url, post_params=post_params, headers=headers)
        if resp.get("refresh_token") is None:
            raise RuntimeError("arc refresh_token not found in response")
        return resp.get("refresh_token")


class SkrHttpConnector:
    @staticmethod
    def get_released_jwt_key(akv_endpoint, maa_endpoint, kid, skr_port, access_token):
        HttpConnector.wait_for_it("localhost", skr_port)
        url = f"http://localhost:{skr_port}/key/release"
        post_params = {
            "maa_endpoint": maa_endpoint,
            "akv_endpoint": akv_endpoint,
            "kid": kid,
            "access_token": access_token,
        }
        headers = {"Content-Type": "application/json"}
        resp = HttpConnector.httppost(url=url, json_data=post_params, headers=headers)
        if resp.get("key") is None:
            raise RuntimeError(f"key absent in the acr response")
        return resp.get("key")


class GovernanceHttpConnector:
    @staticmethod
    def put_event(message, governance_port):
        HttpConnector.wait_for_it("localhost", governance_port)
        url = f"http://localhost:{governance_port}/events"
        data = {"source": "code-launcher", "message": message}
        headers = {"Content-Type": "application/json"}
        HttpConnector.httpput(url, json_data=data, headers=headers)
