import argparse
import json
import os


def generate_arm_template(container_spec_path: str) -> str:
    # We first expand env vars and then JSON deserialize to handle non-string
    # fields. If a non-string field (say, an integer) is to be sourced as an env variable
    # JSON deserialization will fail as the env var placeholder is not surrounded by
    # quotes.
    with open(container_spec_path, "r") as fp:
        container_spec = os.path.expandvars(fp.read())
        container_spec = json.loads(container_spec)

    with open("infra/aci/aci-base-arm-template.json", "r") as fp:
        base_template = os.path.expandvars(fp.read())
        base_template = json.loads(base_template)
        base_template["resources"][0]["properties"] = container_spec

    return json.dumps(base_template, indent=4)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate ARM template")

    parser.add_argument(
        "--container-spec-path",
        help="The path to the container specification.",
        required=True,
        type=str,
    )

    parser.add_argument(
        "--user-assigned-identity-id",
        help="The user assigned identity ARM ID.",
        required=True,
        type=str,
    )

    parser.add_argument(
        "--image-tag", help="The tag to be used for images.", required=True, type=str
    )

    args = parser.parse_args()

    # Setting these up as environment variables in order to make substitution in the
    # ARM template easier.
    os.environ["USER_ASSIGNED_IDENTITY_ID"] = args.user_assigned_identity_id
    os.environ["TAG"] = args.image_tag

    print(generate_arm_template(args.container_spec_path))
