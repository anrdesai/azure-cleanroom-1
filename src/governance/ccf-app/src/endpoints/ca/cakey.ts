import * as ccfapp from "@microsoft/ccf-app";
import { ccf, RsaOaepAesKwpParams } from "@microsoft/ccf-app/global";
import {
  UpdateCertRequest,
  CAKeyItem,
  ReleaseSigningKeyRequest,
  ReleaseSigningKeyResponse
} from "../../models";
import { ErrorResponse } from "../../models/errorresponse";
import { getLastGenerateRequestId, isCAEnabled } from "./ca";
import { getSigningKey } from "../oidc/signingkey";
import {
  SnpAttestationResult,
  verifySnpAttestation
} from "../../attestation/snpattestation";
import { verifyReportData } from "../../utils/utils";
import { Base64 } from "js-base64";

const caSigningKeyStore = ccfapp.typedKv(
  "ca.signing_keys",
  ccfapp.string,
  ccfapp.json<CAKeyItem>()
);

export function getCASigningKey(contractId: string): CAKeyItem {
  if (caSigningKeyStore.has(contractId)) {
    return caSigningKeyStore.get(contractId);
  }

  return null;
}

export function exposeCASigningKey(
  request: ccfapp.Request
): ccfapp.Response<CAKeyItem> | ccfapp.Response<ErrorResponse> {
  const contractId = request.params.contractId;
  if (caSigningKeyStore.has(contractId)) {
    return {
      statusCode: 200,
      body: caSigningKeyStore.get(contractId)
    };
  }

  return {
    statusCode: 404,
    body: new ErrorResponse(
      "NoSigningKey",
      "No signing key is present for the CA."
    )
  };
}

export function releaseCASigningKey(
  request: ccfapp.Request<ReleaseSigningKeyRequest>
): ccfapp.Response<ReleaseSigningKeyResponse> | ccfapp.Response<ErrorResponse> {
  const contractId = request.params.contractId;
  const body = request.body.json();
  if (!body.attestation) {
    return {
      statusCode: 400,
      body: new ErrorResponse(
        "AttestationMissing",
        "Attestation payload must be supplied."
      )
    };
  }

  if (!body.encrypt) {
    return {
      statusCode: 400,
      body: new ErrorResponse(
        "EncryptionMissing",
        "Encrypt payload must be supplied."
      )
    };
  }

  // First validate attestation report.
  let snpAttestationResult: SnpAttestationResult;
  try {
    snpAttestationResult = verifySnpAttestation(contractId, body.attestation);
  } catch (e) {
    return {
      statusCode: 400,
      body: new ErrorResponse("VerifySnpAttestationFailed", e.message)
    };
  }

  //  Then validate the report data value.
  try {
    verifyReportData(snpAttestationResult, body.encrypt.publicKey);
  } catch (e) {
    return {
      statusCode: 400,
      body: new ErrorResponse("ReportDataMismatch", e.message)
    };
  }

  // Attestation report and report data values are verified.
  // Now get the signing key and wrap it with the encryption key before returning it.
  const item = getCASigningKey(contractId);
  if (item == null) {
    return {
      statusCode: 404,
      body: new ErrorResponse(
        "NoSigningKey",
        "No signing key is present for the CA."
      )
    };
  }

  // Wrap the response before returning it.
  const serializedItem = JSON.stringify(item);
  const wrapAlgo = {
    name: "RSA-OAEP-AES-KWP",
    aesKeySize: 256
  } as RsaOaepAesKwpParams;
  const wrapped: ArrayBuffer = ccf.crypto.wrapKey(
    ccf.strToBuf(serializedItem),
    ccf.strToBuf(Base64.decode(body.encrypt.publicKey)),
    wrapAlgo
  );
  const wrappedBase64 = Base64.fromUint8Array(new Uint8Array(wrapped));
  return { body: { value: wrappedBase64 } };
}

export function updateCACert(
  request: ccfapp.Request<UpdateCertRequest>
): ccfapp.Response | ErrorResponse {
  const contractId = request.params.contractId;
  const item = getCASigningKey(contractId);
  if (item === null) {
    return {
      statusCode: 404,
      body: new ErrorResponse(
        "NoCASigningKey",
        "Cannot set certificate as there is no CA signing key for this contract."
      )
    };
  }

  item.caCert = request.body.json().caCert;
  caSigningKeyStore.set(contractId, item);
  return { statusCode: 200 };
}

export function generateCASigningKey(request: ccfapp.Request): ccfapp.Response {
  const contractId = request.params.contractId;
  // Generate a signing key only if feature is enabled and a request to create a signing key
  // was proposed. We only want to create a signing key when signaled by the acceptance of the
  // enable_ca/enable_rotate_ca proposal. This is tracked via
  // matching the requestId value kept in the governance table with the value kept in the private
  // signing key table. Repeated invocations of this API when there is no change in the requestId
  // will be ignored.
  if (!isCAEnabled(contractId)) {
    return {
      statusCode: 405,
      body: new ErrorResponse(
        "CANotEnabled",
        "CA support has not been enabled for this contract."
      )
    };
  }

  const requestId = getLastGenerateRequestId(contractId);
  const existingItem = caSigningKeyStore.get(contractId);
  if (existingItem !== undefined && existingItem.requestId == requestId) {
    return { statusCode: 200, body: { requestId: requestId } };
  }

  const pair = ccf.crypto.generateRsaKeyPair(2048);
  const item: CAKeyItem = {
    requestId: requestId,
    publicKey: pair.publicKey,
    privateKey: pair.privateKey,
    caCert: "<placeholder>"
  };

  // TODO (gsinha): Logic here needs to also generate the certificate using the above key pair
  // once support for generating certificates gets added in CCF. Then remove placeholder value for
  // caCert above with the actual cert.

  caSigningKeyStore.set(contractId, item);
  return { statusCode: 200, body: { requestId: requestId } };
}
