"""Passive source binding checks only: never calls a collector main/build/VM."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import sys

sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
NATIVE = "tools/Native/"
OWNED = "tools/Runtime/Erlang.OwnedGuest506Observations"
LEGACY = ["Erlang.NifMapFamily504Observations", "Erlang.NifMapMaterialized505Observations",
          "Erlang.NifMapReentry505Observations"]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def require(value, message):
    if not value:
        raise ValueError(message)


def module(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    loaded = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(loaded)
    return loaded


def plan(root, relative):
    path = root / relative
    value = json.loads(path.read_bytes())
    require(value.get("PinnedCommit", value.get("PinnedOtpCommit")) == PIN, "Wrong source pin")
    return path, value


def native(root, name, workflow):
    tool = root / NATIVE / name
    path, value = plan(root, NATIVE + name + "/source-plan.json")
    lines = [line for line in workflow.splitlines() if NATIVE + name + "/collect.py" in line]
    require(len(lines) == 1, "One exact workflow collector invocation required")
    match = re.search(r"--plan-sha256 ([0-9A-F]{64})", lines[0])
    require(match is not None and match.group(1) == digest(path), "Workflow source-plan pin differs")
    collector = module(tool / "collect.py", "preflight_" + name.replace(".", "_"))
    if name == LEGACY[1]:
        rows = collector.sources(root, tool, value)
    elif name == LEGACY[2]:
        rows = collector.dependencies(root, value, False)
    else:
        rows = collector.dependencies(root, value)
    for prefix, filename in [("NativeSource", None), ("ErlangSource", None),
                             ("BuildScript", "build-native.py"), ("Collector", "collect.py")]:
        source = tool / filename if filename else root / value[prefix + "Path"]
        require(digest(source) == value[prefix + "Sha256"], "Tool source seal differs: " + str(source))
    if "SealerSha256" in value:
        require(digest(tool / "seal.py") == value["SealerSha256"], "Sealer source differs")
    # Whole owner/env partial membership, including newly added partials.
    paths = {row["Path"] for row in value["Dependencies"]}
    folder = root / "src/Runtime/Erlang.Runtime.Core/Native/Nif"
    required = {p.relative_to(root).as_posix() for pattern in
                ["NifNativeLibraryOwner*.cs", "NifNativeCallEnvironment*.cs"] for p in folder.glob(pattern)}
    require(required.issubset(paths), "Unbound owner/environment partials: " + str(sorted(required - paths)))
    return dict(PlanSha256=digest(path), VerifiedRows=len(rows), NativeExecuted=False)


def pending(root):
    relative = NATIVE + "Erlang.NifPendingVarargsObservations"
    path, value = plan(root, relative + "/reviewed-source-plan.json")
    workflow = (root / ".github/workflows/nif-pending-native.yml").read_text(encoding="utf-8-sig")
    match = re.search(r"REVIEWED_SOURCE_PLAN_SHA256: ([0-9A-F]{64})", workflow)
    require(match is not None and match.group(1) == digest(path), "Pending workflow pin differs")
    helper = module(root / relative / "run-reviewed.py", "preflight_pending")
    names = set()
    for row in value["Files"]:
        require(row["Path"] not in names, "Duplicate pending source")
        names.add(row["Path"])
        source = (root / row["Path"]).resolve()
        require(source.is_relative_to(root), "Pending source escapes repository")
        actual = helper.record(source, root, row["HashMode"])
        actual["Path"] = actual["Path"].replace("\\", "/")
        require(actual == row, "Pending source differs: " + row["Path"])
    # reviewed_inputs invokes git membership enumeration: deliberately not called
    # by this byte-only preflight. The existing native execution gate keeps it.
    return dict(PlanSha256=digest(path), VerifiedRows=len(names), CompleteGitMembershipChecked=False)


def reallocation(root):
    relative = NATIVE + "Erlang.NifReallocationObservations"
    path, _ = plan(root, relative + "/SOURCE-PLAN.json")
    workflow = (root / ".github/workflows/nif-reallocation-native.yml").read_text(encoding="utf-8-sig")
    match = re.search(r"REVIEWED_REALLOCATION_PLAN_SHA256: ([0-9A-F]{64})", workflow)
    require(match is not None and match.group(1) == digest(path), "Reallocation workflow pin differs")
    helper = module(root / relative / "reviewed_common.py", "preflight_reallocation")
    value = helper.verified_plan(root, path, match.group(1))
    return dict(PlanSha256=digest(path), VerifiedRows=len(value["Files"]))


def resource529(root):
    relative = NATIVE + "Erlang.NifResource529Observations"
    path, value = plan(root, relative + "/source-plan.json")
    helper = module(root / relative / "collect.py", "preflight_resource529")
    names = set()
    for row in value["Dependencies"]:
        require(row["Path"] not in names, "Duplicate resource529 source")
        names.add(row["Path"])
        target = (root / row["Path"]).resolve()
        require(target.is_relative_to(root), "Resource529 source escapes repository")
        require(helper.dependency_sha(row) == row["Sha256"], "Resource529 source differs: " + row["Path"])
    require(digest(root / relative / "build-native.py") == value["BuildScriptSha256"], "Resource529 build script differs")
    for variant in value["Variants"].values():
        for prefix in ["NativeSource", "ErlangSource"]:
            require(digest(root / variant[prefix + "Path"]) == variant[prefix + "Sha256"], "Resource529 variant source differs")
    return dict(PlanSha256=digest(path), VerifiedRows=len(names), NativeExecuted=False)


def focused_inputs(root, name):
    tool = root / "tools/Runtime" / name
    path = tool / "inputs.json"
    value = json.loads(path.read_bytes())
    require(value.get("Pin", value.get("SourcePin")) == PIN, "Wrong focused source pin")
    names = set()
    for row in value["Inputs"]:
        require(row["Path"] not in names, "Duplicate focused input: " + row["Path"])
        names.add(row["Path"])
        target = (root / row["Path"]).resolve()
        require(target.is_relative_to(root), "Focused input escapes repository")
        require(digest(target) == row["Sha256"], "Focused input differs: " + row["Path"])
    if "SourceFreezePath" in value:
        frozen_path = (root / value["SourceFreezePath"]).resolve()
        require(frozen_path.is_relative_to(root), "Focused freeze escapes repository")
        require(digest(frozen_path) == value["SourceFreezeSha256"], "Focused freeze hash differs")
        frozen = json.loads(frozen_path.read_bytes())
        require(len(frozen["Rows"]) == value["SourceFreezeRows"], "Focused freeze count differs")
        for row in frozen["Rows"]:
            target = (root / row["Path"]).resolve()
            require(target.is_relative_to(root), "Frozen source escapes repository")
            require(digest(target) == row["Sha256"], "Focused frozen source differs: " + row["Path"])
    if "RunnerSha256" in value:
        require(digest(tool / "run.py") == value["RunnerSha256"], "Focused runner differs")
    return dict(PlanSha256=digest(path), VerifiedRows=len(names), BuildOrVmExecuted=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path(os.environ.get("GITHUB_WORKSPACE", ".")))
    parser.add_argument("--output", type=Path, default=Path("artifacts/source-plan566/preflight.json"))
    args = parser.parse_args()
    root = args.repository.resolve()
    output = args.output.resolve() if args.output.is_absolute() else root / args.output
    require(output.resolve().is_relative_to(root / "artifacts") and not output.exists(), "Fresh artifact output required")
    owned = module(root / OWNED / "collect.py", "preflight_owned506")
    workflow = (root / ".github/workflows/validate.yml").read_text(encoding="utf-8-sig")
    checks = [(name, lambda name=name: native(root, name, workflow)) for name in LEGACY]
    for relative in [OWNED, NATIVE + "Erlang.NifVariadic526Observations"]:
        def inputs(relative=relative):
            path, value = plan(root, relative + "/source-plan.json")
            owned.check_inputs(root, root / relative, value)
            return dict(PlanSha256=digest(path), VerifiedRows=len(value["Inputs"]))
        checks.append((relative, inputs))
    checks.extend([("Pending", lambda: pending(root)), ("Reallocation", lambda: reallocation(root))])
    def private():
        relative = NATIVE + "Erlang.NifPrivateData557Observations"
        path, _ = plan(root, relative + "/source-plan.json")
        evidence = module(root / relative / "evidence.py", "preflight_private557")
        files = evidence.package(root, root / relative, digest(path))
        return dict(PlanSha256=digest(path), VerifiedFiles=len(files))
    checks.append(("Private557", private))
    checks.append(("Resource529", lambda: resource529(root)))
    focused = ["Erlang.Dictionary537Observations", "Erlang.MatchSaved634Observations",
               "Erlang.EtsDispatch540Observations", "Erlang.MonitorOriginal645Observations",
               "Erlang.BeamFunction542Observations"]
    checks.extend((name, lambda name=name: focused_inputs(root, name)) for name in focused)
    outcomes = []
    for name, check in checks:
        try:
            outcomes.append(dict(Plan=name, Passed=True, Details=check()))
        except Exception as error:
            outcomes.append(dict(Plan=name, Passed=False, Error=repr(error)))
    passed = all(row["Passed"] for row in outcomes)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes((json.dumps(dict(Passed=passed, Checks=outcomes, ExpectedPlans=len(checks), ExpectedNativePlans=9, FocusedInputPlans=5,
        BuildOrVmExecuted=False, NativeExecuted=False, SourceHeadAuthenticityClaimed=False,
        FullSourceMembershipClaimed=False, WholeContractsClosed=0), indent=2) + "\n").encode())
    require(passed, "Source-plan consistency failed; all independent results retained")


if __name__ == "__main__":
    main()