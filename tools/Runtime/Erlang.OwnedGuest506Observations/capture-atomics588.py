"""Acquire the genuine atomics worker; strict588 stays default, own-reference590 is explicit."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import sys
import subprocess
import time

sys.dont_write_bytecode = True
TOOL = "tools/Runtime/Erlang.OwnedGuest506Observations"
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
JOB = "atomics-magic586"


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def save(path, value):
    path.write_bytes((json.dumps(value, indent=2) + "\n").encode())


def run_managed(repo, command, directory, environment):
    if os.name == "nt":
        helper = load("atomics592_windows", repo / "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py")
        return helper.owned(command, directory, "managed", 240, environment, str(repo))
    helper = load("atomics592_linux", repo / "tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py")
    process = None
    errors = []
    try:
        process = helper.OwnedProcess(command, repo, environment, directory / "managed")
        process.wait(time.monotonic() + 240, lambda: False)
    except Exception as error:
        errors.append(repr(error))
    finally:
        if process is not None:
            try:
                process.close()
            except Exception as error:
                errors.append("cleanup: " + repr(error))
    receipt = directory / "managed.process.json"
    if not receipt.is_file():
        raise RuntimeError("Managed owned-process receipt absent: " + repr(errors))
    observed = json.loads(receipt.read_bytes())
    if errors or observed["ExitCode"] != 0 or not observed["ParentReaped"] or not observed["StreamsClosed"] or observed["CleanupFailure"] is not None:
        raise RuntimeError("Managed owned-process execution/cleanup failed: " + repr(errors))
    return observed


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--expected-head", required=True)
    parser.add_argument("--source-plan-sha256", required=True)
    provider = parser.add_mutually_exclusive_group(required=True)
    provider.add_argument("--official-admission", type=Path)
    provider.add_argument("--build-receipt", type=Path)
    parser.add_argument("--otp-source", type=Path)
    parser.add_argument("--admission-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--managed-host", type=Path)
    parser.add_argument("--managed-host-sha256")
    parser.add_argument("--metadata-completion", type=Path)
    parser.add_argument("--metadata-completion-sha256")
    parser.add_argument("--own-reference-protocol", action="store_true")
    args = parser.parse_args()
    repository = args.repository.resolve()
    tool = Path(__file__).resolve().parent
    custody = load("atomics592_closure", tool / "atomics-closure592.py")
    require, digest = custody.require, custody.digest
    require(tool == repository / TOOL, "Integrated tool path required")
    require((args.official_admission is not None and os.name == "nt" and args.otp_source is None)
            or (args.build_receipt is not None and sys.platform.startswith("linux") and args.otp_source is not None),
            "Explicit matching Windows official or Linux source-build provider required")
    output = args.output.resolve()
    require(output.is_relative_to(repository / "artifacts") and not output.exists(), "Fresh confined output required")
    plan = tool / "source-plan.json"
    require(digest(plan) == args.source_plan_sha256.upper(), "Source plan differs")
    source = json.loads(plan.read_bytes())
    require(source["PinnedCommit"] == PIN and source["ToolPath"] == TOOL, "Source identity differs")
    bound = {row["Path"]: row for row in source["Inputs"] if row["Scope"] == "Tool"}
    for name in ("capture-atomics588.py", "atomics-closure592.py"):
        require(name in bound and bound[name]["HashMode"] == "Raw" and bound[name]["Sha256"] == digest(tool / name),
                "Executable source not exact raw bound: " + name)
    if args.managed_host:
        args.managed_host = args.managed_host.resolve()
        require(args.managed_host.is_relative_to(repository) and args.managed_host.is_file()
                and args.managed_host_sha256 and digest(args.managed_host) == args.managed_host_sha256.upper()
                and args.metadata_completion is not None and args.metadata_completion_sha256,
                "Actual final managed host and metadata admission required")
        require(digest(args.metadata_completion) == args.metadata_completion_sha256.upper(), "Metadata completion differs")
    else:
        require(not args.own_reference_protocol, "Own-reference policy requires an actual managed replay")
    output.mkdir(parents=True)
    errors, runs = [], []
    before = None
    if args.managed_host:
        try:
            before = custody.managed_before(args.managed_host, output)
            save(output / "managed-inputs-before.json", before)
        except Exception as error:
            errors.append("managed preparation: " + repr(error))
    original = output / "original"
    command = [sys.executable, "-B", str(tool / "collect.py"), "--repository", str(repository),
               "--expected-head", args.expected_head, "--plan-sha256", args.source_plan_sha256,
               "--admission-sha256", args.admission_sha256, "--output", str(original),
               "--execute-original", "--compiled-worker", "--job", JOB]
    if args.official_admission:
        command.extend(["--official-admission", str(args.official_admission.resolve())])
    else:
        command.extend(["--build-receipt", str(args.build_receipt.resolve()), "--otp-source", str(args.otp_source.resolve())])
    original_code = None
    try:
        with (output / "original.stdout").open("xb") as stdout, (output / "original.stderr").open("xb") as stderr:
            original_code = subprocess.run(command, cwd=repository, stdout=stdout, stderr=stderr, check=False).returncode
        require(original_code == 0, "Original batch failed; adoption/replay skipped")
    except Exception as error:
        errors.append("original: " + repr(error))
    admission = output / "admission.json"
    adopted = False
    if original_code == 0 and args.managed_host and before is not None:
        completion = original / "completion.json"
        command = [sys.executable, "-B", str(tool / "adopt-atomics588.py"), "--repository", str(repository),
                   "--completion", str(completion), "--completion-sha256", digest(completion),
                   "--source-plan", str(plan), "--source-plan-sha256", args.source_plan_sha256,
                   "--output", str(admission)]
        try:
            with (output / "adoption.stdout").open("xb") as stdout, (output / "adoption.stderr").open("xb") as stderr:
                adopted = subprocess.run(command, cwd=repository, stdout=stdout, stderr=stderr, check=False).returncode == 0
            require(adopted, "Source-bound adoption failed; managed replay skipped")
        except Exception as error:
            errors.append("adoption: " + repr(error))
    if adopted:
        original_admission = json.loads(admission.read_bytes())
        for owned in (False, True):
            profile = "private-owned" if owned else "ordinary"
            logs = output / profile
            logs.mkdir()
            capture = logs / "actual"
            row = dict(Profile=profile, RequestedOwned=owned, Passed=False, Receipt=None, Error=None)
            command = [before["DotnetPath"], "exec", "--fx-version", custody.VERSION, str(args.managed_host),
                       "--original-atomics590-identity-child" if args.own_reference_protocol else "--original-atomics588-child",
                       str(repository), str(admission), digest(admission), str(args.metadata_completion.resolve()),
                       args.metadata_completion_sha256, str(owned).lower(), str(capture)]
            save(logs / "request.json", dict(Profile=profile, Command=command, OwnReferenceProtocolOptIn=args.own_reference_protocol))
            try:
                custody.managed_after(before)
                environment = dict(os.environ, DOTNET_HOST_TRACE="1", DOTNET_HOST_TRACE_VERBOSITY="3",
                                   DOTNET_HOST_TRACEFILE=str(logs / "dotnet-host.trace"))
                row["Receipt"] = run_managed(repository, command, logs, environment)
                comparison = capture / "comparison.json"
                require(comparison.is_file(), "Actual child comparison missing")
                proof = json.loads(comparison.read_bytes())
                custody.validate_comparison(proof, owned, args.own_reference_protocol)
                worker = original_admission["Worker"]
                require(proof["SourcePin"] == PIN and proof["SubjectBeamSha256"] == worker["BeamSha256"]
                        and proof["WorkerBeamSha256"] == worker["WorkerBeamSha256"]
                        and proof["WorkerSourceSha256"] == worker["WorkerSourceSha256"], "Executed worker provenance differs")
                require([(v["Name"], v["OriginalBeamSha256"]) for v in proof["StockImages"]]
                        == [(v["Module"], v["BeamSha256"]) for v in original_admission["Stock"]], "Executed stock image provenance differs")
                require((logs / "dotnet-host.trace").is_file(), "Actual runtime-host trace missing")
                custody.managed_after(before)
                row.update(Passed=True, ComparisonSha256=digest(comparison),
                           WholeDefaultEtfEqual=proof["WholeDefaultEtfEqual"], WholeDeterministicEtfEqual=proof["WholeDeterministicEtfEqual"],
                           OwnReferenceProtocolPassed=proof["OwnReferenceProtocolPassed"],
                           OwnReferenceDefault=proof.get("OwnReferenceDefault"), OwnReferenceDeterministic=proof.get("OwnReferenceDeterministic"))
            except Exception as error:
                row["Error"] = repr(error)
                errors.append(profile + ": " + repr(error))
                for name in ("managed.exit.json", "managed.process.json"):
                    if (logs / name).is_file():
                        row["Receipt"] = json.loads((logs / name).read_bytes())
                        break
            try:
                row["Artifacts"] = custody.files(logs)
            except Exception as error:
                row["Passed"] = False
                row["ArtifactError"] = repr(error)
                errors.append(profile + " artifact inventory: " + repr(error))
            runs.append(row)
    if args.managed_host and before is not None:
        try:
            save(output / "managed-inputs-after.json", custody.managed_after(before))
            require(digest(args.metadata_completion) == args.metadata_completion_sha256.upper(), "Metadata input drifted")
        except Exception as error:
            errors.append("managed postflight: " + repr(error))
    if args.own_reference_protocol and len(runs) == 2 and all(row["Passed"] for row in runs):
        try:
            require(runs[0]["OwnReferenceDefault"]["OriginalRole"] == runs[1]["OwnReferenceDefault"]["OriginalRole"],
                    "Profiles did not use the same admitted original reference role")
        except Exception as error:
            errors.append("cross-profile protocol: " + repr(error))
    try:
        require(digest(plan) == args.source_plan_sha256.upper(), "Final source-plan drift")
    except Exception as error:
        errors.append("source postflight: " + repr(error))
    inventory_sha = None
    try:
        inventory = custody.files(output)
        save(output / "artifacts-before-completion.json", dict(Files=inventory))
        inventory_sha = digest(output / "artifacts-before-completion.json")
    except Exception as error:
        errors.append("final artifact inventory: " + repr(error))
    final = dict(Passed=not errors, Errors=errors, OriginalExitCode=original_code, ManagedRequested=bool(args.managed_host),
                 ExpectedHead=args.expected_head, SourcePin=PIN, ProviderKind="OfficialWindows" if args.official_admission else "SourceBuiltLinux",
                 ProviderAdmissionSha256=args.admission_sha256.upper(), MetadataCompletionSha256=args.metadata_completion_sha256,
                 ManagedPreparationPassed=before is not None if args.managed_host else None, AdoptionPassed=adopted,
                 ManagedRuns=runs, ExpectedManagedRoutes=2 if args.managed_host else 0,
                 CompletedManagedRoutes=len(runs), OwnReferenceProtocolOptIn=args.own_reference_protocol,
                 ComparisonPolicy=custody.OWNREF if args.own_reference_protocol else custody.STRICT,
                 EachManagedVmHasOwnReferenceBinding=True, RawEtfRewritten=False, WholeContractsClosed=0,
                 ManagedComparisonPerformed=bool(runs), ManagedReplaySkipped=bool(args.managed_host) and not adopted,
                 ArtifactInventorySha256=inventory_sha,
                 ManagedHostSha256=args.managed_host_sha256, SourcePlanSha256=args.source_plan_sha256.upper())
    save(output / "completion.json", final)
    require(not errors, "; ".join(errors))


if __name__ == "__main__":
    main()
