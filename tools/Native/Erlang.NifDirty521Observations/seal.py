"""Seal a fresh dirty521 original acquisition plan; no build or VM is invoked."""
import argparse
import hashlib
import json
from pathlib import Path

PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
TARGET = "tools/Native/Erlang.NifDirty521Observations"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def plan(repository, tool):
    baseline_path = repository / "tools/Native/Erlang.NifMapFamily504Observations/source-plan.json"
    previous = json.loads(baseline_path.read_bytes())
    variants = {
        "flag0": {"Value": 0, "Define": "0u"},
        "flag1": {"Value": 1, "Define": "1u"},
        "flag2": {"Value": 2, "Define": "2u"},
        "flag3": {"Value": 3, "Define": "3u"},
        "flag4": {"Value": 4, "Define": "4u"},
        "flag80000000": {"Value": 2147483648, "Define": "0x80000000u"},
        "flagffffffff": {"Value": 4294967295, "Define": "0xffffffffu"},
    }
    jobs = {
        "whole": {"Variant": "flag1", "Mode": "whole", "ExpectedLoad": "loaded"},
    }
    for kind in ("cpu", "io"):
        for lifecycle in ("purge", "kill"):
            jobs["control-" + kind + "-" + lifecycle] = {
                "Variant": "flag1",
                "Mode": "control",
                "Kind": kind,
                "Lifecycle": lifecycle,
            }
    for variant, values in variants.items():
        jobs["probe-" + variant] = {
            "Variant": variant,
            "Mode": "probe",
            "ExpectedLoad": "loaded" if values["Value"] in (0, 1, 2) else "bad_lib",
        }

    own_names = (".gitattributes", "build-native.py", "collect.py", "seal.py", "README.md")
    own = [tool / name for name in own_names]
    own.extend(sorted((tool / "fixtures").glob("*")))
    dependencies = []
    for path in own:
        dependencies.append({
            "Path": TARGET + "/" + path.relative_to(tool).as_posix(),
            "Sha256": sha(path),
            "HashMode": "Raw",
        })
    external = (
        "tools/Native/Erlang.NifMapFamily504Observations/build-native.py",
        "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py",
        "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/bounded_inventory.py",
    )
    for name in external:
        # The existing original-only collector checks these exact raw bytes.
        dependencies.append({
            "Path": name,
            "Sha256": sha(repository / name),
            "HashMode": "Raw",
        })

    return {
        "PinnedCommit": PIN,
        "Platform": "windows-x64",
        "OriginalOnly": True,
        "NativeSourcePath": TARGET + "/fixtures/dirty_flags521.c",
        "NativeSourceSha256": sha(tool / "fixtures/dirty_flags521.c"),
        "ErlangSourcePath": TARGET + "/fixtures/dirty_flags521.erl",
        "ErlangSourceSha256": sha(tool / "fixtures/dirty_flags521.erl"),
        "BuildScriptSha256": sha(tool / "build-native.py"),
        "Headers": previous["Headers"],
        "Variants": variants,
        "Jobs": jobs,
        "Dependencies": dependencies,
        "ExpectedNativeBuilds": 7,
        "ExpectedFreshOriginalVMs": 12,
        "ProviderFiles": 6427,
        "ProviderArguments": ["+S", "1:1", "+A", "1", "+SDcpu", "1", "+SDio", "2"],
        "WholeContractsPromoted": 0,
        "ManagedComparisonsExecuted": False,
        "OriginalGoldenState": "NOT_COLLECTED_BY_THIS_PROPOSAL",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path(__file__).resolve().parents[3])
    arguments = parser.parse_args()
    tool = Path(__file__).resolve().parent
    value = plan(arguments.repository.resolve(), tool)
    target = tool / "source-plan.json"
    target.write_bytes((json.dumps(value, indent=2) + "\n").encode("utf-8"))
    print(json.dumps({
        "Path": str(target),
        "Sha256": sha(target),
        "BuildExecuted": False,
        "OriginalExecuted": False,
    }))


if __name__ == "__main__":
    main()
