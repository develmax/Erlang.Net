"""Admit an already completed, source-bound original boxed-float worker; starts no VM."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
TOOL = "tools/Runtime/Erlang.OwnedGuest506Observations"
JOB = "boxed-float556"


def sha(path, mode="Raw"):
    data = path.read_bytes()
    if mode == "TextLF":
        data = data.replace(b"\r\n", b"\n")
    elif mode != "Raw":
        raise ValueError("Unsupported source hash mode")
    return hashlib.sha256(data).hexdigest().upper()


def require(value, message):
    if not value:
        raise ValueError(message)


def admit_source(repository, tool, source_plan, expected):
    require(source_plan == tool / "source-plan.json", "Exact integrated tool plan required")
    require(sha(source_plan) == expected.upper(), "Source plan SHA differs")
    plan = json.loads(source_plan.read_bytes())
    require(plan["PinnedCommit"] == PIN and plan["ToolPath"] == TOOL, "Source plan identity differs")
    identities = set()
    for row in plan["Inputs"]:
        require(row["Scope"] in {"Tool", "Repository"}, "Unknown source scope")
        root = tool if row["Scope"] == "Tool" else repository
        path = (root / row["Path"]).resolve()
        require(path.is_relative_to(root) and path.is_file(), "Source input escapes or is absent")
        identity = (row["Scope"], row["Path"])
        require(identity not in identities, "Duplicate source input")
        identities.add(identity)
        require(sha(path, row["HashMode"]) == row["Sha256"], "Source input SHA differs: " + row["Path"])
    required = {("Tool", name) for name in (
        "adopt-float556.py", "admitted_receipts.py", "collect.py", "jobs.json",
        "fixtures/boxed_float556.erl", "fixtures/contract_worker513.erl")}
    require(required.issubset(identities), "Adoption helper/worker/fixture not source-bound")
    for row in plan["Inputs"]:
        if (row["Scope"], row["Path"]) in required:
            require(row["HashMode"] == "Raw", "Adoption executable/fixture requires exact raw bytes")
    return plan


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--completion", type=Path, required=True)
    parser.add_argument("--completion-sha256", required=True)
    parser.add_argument("--source-plan", type=Path, required=True)
    parser.add_argument("--source-plan-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    repository = args.repository.resolve()
    tool = Path(__file__).resolve().parent
    require(tool == repository / TOOL, "Adopter must use integrated tool-owned sources")
    source_plan = args.source_plan.resolve()
    admit_source(repository, tool, source_plan, args.source_plan_sha256)
    completion = args.completion.resolve()
    output = args.output.resolve()
    require(completion.is_relative_to(repository / "artifacts"), "Completion must be a repository artifact")
    require(output.is_relative_to(repository / "artifacts") and not output.exists(), "Fresh repository artifact output required")
    helper = tool / "admitted_receipts.py"
    spec = importlib.util.spec_from_file_location("owned506_admitted_receipts", helper)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    admitted = module.original_job(completion, args.completion_sha256.upper(), args.source_plan_sha256.upper(), PIN, JOB)
    receipt = json.loads(completion.read_bytes())
    require(not receipt.get("Errors") and not receipt["Phases"][JOB].get("TimedOut", False), "Original acquisition failed or timed out")
    files = admitted["Artifacts"]
    for name in ("boxed_float556.erl", "contract_worker513.erl"):
        require(files[name].read_bytes() == (tool / "fixtures" / name).read_bytes(), "Compiled original source differs: " + name)
    require("boxed_float556.beam" in files, "Original float producer BEAM missing")
    result = dict(PinnedCommit=PIN, Job=JOB, OriginalAcquisitionPassed=True, SourcePlanSha256=args.source_plan_sha256.upper())
    paths = {
        "Source": files["boxed_float556.erl"],
        "Beam": files["boxed_float556.beam"],
        "WorkerBeam": files["contract_worker513.beam"],
        "WorkerSource": files["contract_worker513.erl"],
        "Raw": files["observed.etf"],
        "Deterministic": files["observed-deterministic.etf"],
        "Completion": completion,
        "Inventory": completion.parent / JOB / "artifacts.json",
        "ProviderBefore": completion.parent / "provider-before.json",
        "ProviderAfter": completion.parent / "provider-after.json",
    }
    for key, path in paths.items():
        resolved = path.resolve()
        require(resolved.is_relative_to(repository), "Adopted evidence escapes repository")
        result[key + "Path"] = resolved.relative_to(repository).as_posix()
        result[key + "Sha256"] = sha(resolved)
    # Check source and completion again before publication; adoption never rewrites ETF.
    admit_source(repository, tool, source_plan, args.source_plan_sha256)
    require(sha(completion) == args.completion_sha256.upper(), "Completion changed during adoption")
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("xb") as stream:
        stream.write((json.dumps(result, indent=2) + "\n").encode("utf-8"))
    print(json.dumps(dict(AdoptionSha256=sha(output), OriginalOrManagedVmExecutedByAdoption=False, WholeContractsClosed=0)))


if __name__ == "__main__":
    main()
