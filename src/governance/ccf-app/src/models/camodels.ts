import { SnpEvidence } from "../attestation/snpattestation";
import { Encrypt } from "./encrypt";

export interface CAConfigItem {
  enabled: string;
  requestId: string;
}

export interface CAKeyItem {
  requestId: string;
  publicKey: string;
  privateKey: string;
  caCert: string;
}

export interface CAInfo {
  enabled: boolean;
  caCert?: string;
  publicKey?: string;
  proposalIds: string[];
}

export interface UpdateCertRequest {
  caCert: string;
}

export interface ReleaseSigningKeyRequest {
  attestation: SnpEvidence;
  encrypt: Encrypt;
}

export interface ReleaseSigningKeyResponse {
  value: string;
}
