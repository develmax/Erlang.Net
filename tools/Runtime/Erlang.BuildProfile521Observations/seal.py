"""Fresh build-profile source seal; no build or VM."""
import argparse
import hashlib
import json
from pathlib import Path
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
TARGET = "tools/Runtime/Erlang.BuildProfile521Observations"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def plan(repository, tool):
    inputs = [{"Scope": "Tool", "Path": p.relative_to(tool).as_posix(), "HashMode": "Raw", "Sha256": sha(p)}
        for p in sorted(tool.rglob("*")) if p.is_file() and p.name != "source-plan.json"]
    for name in ("tools/Runtime/Erlang.OwnedGuest506Observations/collect.py",
                 "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py",
                 "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/bounded_inventory.py"):
        inputs.append({"Scope": "Repository", "Path": name, "HashMode": "Raw", "Sha256": sha(repository / name)})
    return {"PinnedCommit": PIN, "State": "SEALED_SOURCE_NOT_EXECUTION_RECEIPT", "ToolPath": TARGET,
        "Inputs": inputs, "OriginalModule": "build_profile521", "CompiledWorker": "contract_worker513",
        "SelectorOutcomes": 9, "BadInputStacks": 4, "FreshOriginalVMs": 1, "ManagedExecuted": False,
        "RawOutputProjectionOrNormalization": False, "WholeContractsClosed": 0}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    args = parser.parse_args()
    tool = Path(__file__).resolve().parent
    path = tool / "source-plan.json"
    path.write_text(json.dumps(plan(args.repository.resolve(), tool), indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"SourcePlanSha256": sha(path), "OriginalExecuted": False}))


if __name__ == "__main__":
    main()
