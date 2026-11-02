"""Passive Resource529 source-plan admission; never builds or executes guest/native code."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[3]
RELATIVE_TOOL = Path("tools/Native/Erlang.NifResource529Observations")
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def verify_source_plan534(root, plan_path, expected_sha256):
    failures = []
    actual_sha256 = digest(plan_path)
    if actual_sha256 != expected_sha256.upper():
        failures.append(dict(Check="ExactPlanSha256", Path=str(plan_path),
            Expected=expected_sha256.upper(), Actual=actual_sha256))
    plan = json.loads(plan_path.read_bytes())
    for key, expected in [("PinnedCommit", PIN), ("ProtocolSchemaVersion", 2),
            ("OriginalFirstRequired", True), ("SealState", "FINAL_SOURCE_BOUND_NOT_EXECUTED")]:
        if plan.get(key) != expected:
            failures.append(dict(Check=key, Expected=expected, Actual=plan.get(key)))
    for key, expected in [("Variants", {"family1", "handoff1", "handoff2"}),
            ("Jobs", {"family527", "fresh_load_after_purge", "genuine_upgrade"})]:
        actual = plan.get(key)
        if not isinstance(actual, dict) or set(actual) != expected:
            failures.append(dict(Check=key, Expected=sorted(expected), Actual=actual))

    tool = root / RELATIVE_TOOL
    specification = importlib.util.spec_from_file_location("resource529_passive534", tool / "collect.py")
    collector = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(collector)
    # Reuse the actual source collector's mode rules and all-row diagnostics.
    # Importing it only defines helpers; its guarded main is never called.
    collector.ROOT = root
    rows = plan.get("Dependencies")
    failures.extend(dict(Check="Dependency", **error) for error in collector.dependency_failures(rows))
    if isinstance(rows, list):
        counts = {}
        for row in rows:
            if isinstance(row, dict) and isinstance(row.get("Path"), str):
                counts[row["Path"]] = counts.get(row["Path"], 0) + 1
        failures.extend(dict(Check="DuplicateDependency", Path=path, Count=count)
            for path, count in sorted(counts.items()) if count != 1)
        for name in ["collect.py", "build-native.py", "seal.py", "verify-source-plan.py"]:
            relative = (RELATIVE_TOOL / name).as_posix()
            if counts.get(relative, 0) != 1:
                failures.append(dict(Check="RequiredToolDependency", Path=relative,
                    ExpectedCount=1, ActualCount=counts.get(relative, 0)))
    try:
        actual_build = digest(tool / "build-native.py")
        if actual_build != plan.get("BuildScriptSha256"):
            failures.append(dict(Check="BuildScriptSha256", Path=str(tool / "build-native.py"),
                Expected=plan.get("BuildScriptSha256"), Actual=actual_build))
    except OSError as error:
        failures.append(dict(Check="BuildScriptSha256", Error=repr(error)))
    return dict(Passed=not failures, PlanSha256=actual_sha256,
        Dependencies=len(rows) if isinstance(rows, list) else 0, Failures=failures,
        BuildOrVmExecuted=False, NativeExecuted=False, SourcePlanWritten=False,
        FullSourceMembershipClaimed=False, WholeContractsClosed=0)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--plan-sha256", required=True)
    parser.add_argument("--output", type=Path)
    arguments = parser.parse_args()
    try:
        report = verify_source_plan534(ROOT, ROOT / RELATIVE_TOOL / "source-plan.json", arguments.plan_sha256)
    except (OSError, ValueError, KeyError, TypeError, AttributeError) as error:
        report = dict(Passed=False, Failures=[dict(Check="SourcePlanPreparation", Error=repr(error))],
            BuildOrVmExecuted=False, NativeExecuted=False, SourcePlanWritten=False,
            FullSourceMembershipClaimed=False, WholeContractsClosed=0)
    text = json.dumps(report, indent=2) + "\n"
    if arguments.output:
        arguments.output.parent.mkdir(parents=True, exist_ok=True)
        arguments.output.write_text(text, encoding="utf-8")
    print(text, end="")
    return 0 if report["Passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
