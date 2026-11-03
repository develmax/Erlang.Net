"""Acquire one genuine integer worker and optionally replay both runtime profiles.

Uses the existing admitted collector and process owner; no expected ETF is seeded.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys

sys.dont_write_bytecode = True
JOBS = ["boxed-integer559"]
TOOL = "tools/Runtime/Erlang.OwnedGuest506Observations"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def require(value, message):
    if not value:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--expected-head", required=True)
    parser.add_argument("--source-plan-sha256", required=True)
    parser.add_argument("--official-admission", type=Path, required=True)
    parser.add_argument("--admission-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--managed-host", type=Path)
    parser.add_argument("--metadata-completion", type=Path)
    parser.add_argument("--metadata-completion-sha256")
    args = parser.parse_args()
    repository = args.repository.resolve()
    tool = Path(__file__).resolve().parent
    require(tool == repository / TOOL and os.name == "nt", "Integrated Windows tool required")
    output = args.output.resolve()
    require(output.is_relative_to(repository / "artifacts") and not output.exists(), "Fresh repository output required")
    plan = tool / "source-plan.json"
    require(digest(plan) == args.source_plan_sha256.upper(), "Plan SHA differs")
    if args.managed_host:
        require(args.managed_host.is_file() and args.metadata_completion is not None
                and args.metadata_completion_sha256, "Final built host and independently admitted actual metadata receipt required")
    output.mkdir(parents=True)
    original = output / "original"
    command = [sys.executable, "-B", str(tool / "collect.py"), "--repository", str(repository),
               "--expected-head", args.expected_head, "--plan-sha256", args.source_plan_sha256,
               "--official-admission", str(args.official_admission.resolve()), "--admission-sha256", args.admission_sha256,
               "--output", str(original), "--execute-original", "--compiled-worker"]
    for job in JOBS:
        command.extend(["--job", job])
    # The existing collector owns every VM/process deadline and cleanup and
    # aggregates independent job failures before exiting.
    with (output / "original.stdout").open("wb") as stdout, (output / "original.stderr").open("wb") as stderr:
        result = subprocess.run(command, cwd=repository, stdout=stdout, stderr=stderr, check=False)
    runs = []
    errors = []
    if result.returncode != 0:
        errors.append("Original batch failed; dependent adoption/replay skipped")
    elif args.managed_host:
        completion = original / "completion.json"
        admission = output / "admission.json"
        adopt = [sys.executable, "-B", str(tool / "adopt-integer559.py"), "--repository", str(repository),
                 "--completion", str(completion), "--completion-sha256", digest(completion),
                 "--source-plan", str(plan), "--source-plan-sha256", args.source_plan_sha256, "--output", str(admission)]
        with (output / "adoption.stdout").open("wb") as stdout, (output / "adoption.stderr").open("wb") as stderr:
            adopted = subprocess.run(adopt, cwd=repository, stdout=stdout, stderr=stderr, check=False)
        if adopted.returncode != 0:
            errors.append("Adoption failed; managed replay skipped")
        else:
            owner_path = repository / "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py"
            spec = importlib.util.spec_from_file_location("integer559_owned", owner_path)
            owner = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(owner)
            for owned in (False, True):
                profile = "private-owned" if owned else "ordinary"
                logs = output / profile
                logs.mkdir(parents=True)
                capture = logs / "actual"
                command = ["dotnet", "exec", "--fx-version", "10.0.11", str(args.managed_host.resolve()),
                           "--original-integer559-child", str(repository), str(admission), digest(admission),
                           str(args.metadata_completion.resolve()), args.metadata_completion_sha256,
                           str(owned).lower(), str(capture)]
                try:
                    receipt = owner.owned(command, logs, "managed", 240, dict(os.environ), str(repository))
                    passed = receipt.get("ExitCode") == 0 and receipt.get("Settled", receipt.get("ParentReaped", False))
                    comparison = capture / "comparison.json"
                    if passed:
                        require(comparison.is_file(), "Actual profile comparison receipt is missing")
                        proof = json.loads(comparison.read_bytes())
                        require(proof["Passed"] and proof["OwnedProfile"] is owned,
                                "Actual child profile differs from requested route")
                    runs.append(dict(Profile=profile, Passed=passed, Receipt=receipt,
                                     ComparisonSha256=digest(comparison) if comparison.is_file() else None))
                    if not passed:
                        errors.append(profile + " whole comparison failed")
                except Exception as error:
                    errors.append(profile + ": " + repr(error))
    final = dict(Passed=not errors, OriginalExitCode=result.returncode, ManagedRequested=bool(args.managed_host),
                 ManagedRuns=runs, ExpectedManagedRoutes=2 if args.managed_host else 0,
                 Errors=errors, WholeContractsClosed=0,
                 ReferenceBatch="Genuine compiled boxed-integer worker; whole values and error stacks",
                 RawEtfRewritten=False)
    (output / "completion.json").write_bytes((json.dumps(final, indent=2) + "\n").encode())
    require(not errors, "; ".join(errors))


if __name__ == "__main__":
    main()
