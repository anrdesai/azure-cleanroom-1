import argparse
import json
import os
import yaml


def get_containers_registry_url():
    return os.environ.get(
        "AZCLI_CLEANROOM_CONTAINER_REGISTRY_URL",
        "docker.io/gausinha",
    )


parser = argparse.ArgumentParser(
    description="Convert an Clean Room ACI ARM template to a k8s deployment spec for local testing."
)
parser.add_argument(
    "--template-file",
    type=str,
    dest="template_file",
    required=True,
    help="The ARM template file to convert.",
)
parser.add_argument(
    "--out-dir",
    type=str,
    dest="out_dir",
    required=True,
    help="The output directory to create the deployment spec.",
)
parser.add_argument(
    "--name",
    type=str,
    dest="pod_name",
    required=False,
    default="virtual-cleanroom",
    help="The pod name to use in the pod spec. Defaults to virtual-cleanroom.",
)

args = parser.parse_args()

with open(args.template_file) as f:
    parsed_json = json.load(f)

# Map init containers.
pod_init_containers = []
for container in parsed_json["resources"][0]["properties"]["initContainers"]:
    pod_init_container = {
        "name": container["name"],
        "image": container["properties"]["image"],
    }
    if "command" in container["properties"]:
        pod_init_container["command"] = container["properties"]["command"]
    if "securityContext" in container["properties"]:
        if "privileged" in container["properties"]["securityContext"]:
            pod_init_container["securityContext"] = {
                "privileged": (
                    True
                    if container["properties"]["securityContext"]["privileged"]
                    == "true"
                    else False
                )
            }
    pod_init_containers.append(pod_init_container)

# Map containers.
pod_containers = []
for container in parsed_json["resources"][0]["properties"]["containers"]:
    pod_container = {
        "name": container["name"],
        "image": container["properties"]["image"],
    }

    if "command" in container["properties"]:
        pod_container["command"] = container["properties"]["command"]

    privileged = False
    if "securityContext" in container["properties"]:
        if "privileged" in container["properties"]["securityContext"]:
            pod_container["securityContext"] = {
                "privileged": (
                    True
                    if container["properties"]["securityContext"]["privileged"]
                    == "true"
                    or container["properties"]["securityContext"]["privileged"] == True
                    else False
                )
            }
            privileged = pod_container["securityContext"]["privileged"]

    if "environmentVariables" in container["properties"]:
        pod_container["env"] = []
        for item in container["properties"]["environmentVariables"]:
            pod_container["env"].append({"name": item["name"], "value": item["value"]})

    if "volumeMounts" in container["properties"]:
        pod_container["volumeMounts"] = []
        for item in container["properties"]["volumeMounts"]:
            pod_container["volumeMounts"].append(
                {
                    "name": item["name"],
                    "mountPath": item["mountPath"],
                    "mountPropagation": (
                        "Bidirectional" if privileged else "HostToContainer"
                    ),
                }
            )

    pod_containers.append(pod_container)

# Map volumes containers.
volumes = []
if "volumes" in parsed_json["resources"][0]["properties"]:
    volumes = parsed_json["resources"][0]["properties"]["volumes"]

# Replace/update/remove few containers for local testing scenario.
# Remove ccr-attestation as it is not required in local env.
# Replace skr-sidecar with the local-skr container.
# Add mount-point in ccr-governance sidecar to load attestation report/keys.
pod_containers = [pc for pc in pod_containers if pc["name"] != "ccr-attestation"]
pod_containers = [pc for pc in pod_containers if pc["name"] != "skr-sidecar"]
registry_url = get_containers_registry_url()
pod_containers.append(
    {
        "name": "local-skr-sidecar",
        "image": f"{registry_url}/local-skr:latest",
    }
)
for item in pod_containers:
    if item["name"] == "ccr-governance":
        item["env"].append(
            {"name": "insecure_mountpoint", "value": "/app/insecure-virtual/"}
        )
        item["env"].append({"name": "INSECURE_VIRTUAL_ENVIRONMENT", "value": "true"})
        item["volumeMounts"].append(
            {"name": "insecure-virtual", "mountPath": "/app/insecure-virtual"}
        )
volumes.append(
    {
        "name": "insecure-virtual",
        "configMap": {
            "name": "insecure-virtual",
            "items": [
                {"key": "ccr_gov_pub_key", "path": "keys/ccr_gov_pub_key.pem"},
                {"key": "ccr_gov_priv_key", "path": "keys/ccr_gov_priv_key.pem"},
                {
                    "key": "attestation_report",
                    "path": "attestation/attestation-report.json",
                },
            ],
        },
    }
)
output = {
    "apiVersion": "v1",
    "kind": "Pod",
    "metadata": {"name": args.pod_name, "labels": {"app": args.pod_name}},
    "spec": {
        "initContainers": pod_init_containers,
        "containers": pod_containers,
        "volumes": volumes,
        "restartPolicy": "Never",
    },
}

out_file = os.path.join(args.out_dir, f"{args.pod_name}-pod.yaml")
with open(out_file, "w") as yaml_file:
    yaml.dump(output, yaml_file, default_flow_style=False, sort_keys=False)
