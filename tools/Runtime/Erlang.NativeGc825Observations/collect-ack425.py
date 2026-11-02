"""Fresh unchanged425 source/BEAM/full-stack batch; reuse admitted OTP once."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
from types import SimpleNamespace

sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
TOOL = "tools/Runtime/Erlang.NativeGc825Observations"
PROJECT = "tests/Runtime/Erlang.NativeGc.Observations"
FIXTURES = "tests/Erlang.Tests/Fixtures/InternalProcessOriginal425"
TIMEOUT = 45
FRAMEWORK = "10.0.11"


def require(value, message):
    if not value:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def write(path, value):
    with Path(path).open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, indent=2)
        stream.write("\n")


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--collector", type=Path, required=True)
    subject = parser.add_mutually_exclusive_group(required=True)
    subject.add_argument("--expected-head")
    subject.add_argument("--local-source-manifest", type=Path)
    parser.add_argument("--local-source-manifest-sha256")
    parser.add_argument("--inputs-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    admitted = parser.add_mutually_exclusive_group(required=True)
    admitted.add_argument("--official-admission", type=Path)
    admitted.add_argument("--build-receipt", type=Path)
    parser.add_argument("--admission-sha256", required=True)
    parser.add_argument("--otp-source", type=Path)
    args = parser.parse_args()
    repo = args.repository.resolve()
    tool = Path(__file__).resolve().parent
    out = args.output.resolve()
    require(tool == repo / TOOL, "Integrated reviewed tool path required")
    require(out.is_relative_to(repo / "artifacts") and not out.exists(), "Fresh confined output required")
    out.mkdir(parents=True)
    local_rows = None
    if args.local_source_manifest is not None:
        require(args.official_admission is not None,
            "Unpublished local mode currently uses genuine Windows admission only; Linux receipt requires its actual Git admission repository")
        require(args.local_source_manifest_sha256 and sha(args.local_source_manifest) == args.local_source_manifest_sha256.upper(),
            "Explicit local source manifest seal differs")
        local = json.loads(args.local_source_manifest.read_bytes())
        require(Path(local["Root"]).resolve() == repo, "Local manifest belongs to another source tree")
        local_rows = local["Files"]
        names = set()
        for row in local_rows:
            path = (repo / row["Path"]).resolve()
            require(path.is_relative_to(repo) and path.is_file() and not path.is_symlink(), "Invalid local source row")
            require(row["Path"] not in names, "Duplicate local source row")
            names.add(row["Path"])
            require(path.stat().st_size == row["Bytes"] and sha(path) == row["Sha256"], "Local source differs " + row["Path"])
        must = set()
        for part in ("src", "tests", "tools"):
            for root, directories, files in os.walk(repo / part):
                directories[:] = [name for name in directories if name not in {"bin", "obj", ".git"}]
                for name in files:
                    if Path(name).suffix.lower() in {".cs", ".csproj", ".props", ".targets"}:
                        must.add((Path(root) / name).relative_to(repo).as_posix())
        require(must and must <= names, "Local manifest omits actual code/project members: " + repr(sorted(must - names)[:20]))
        require("global.json" in names, "Local SDK binding missing")
        write(out / "local-source-before.json", local_rows)
    else:
        require(subprocess.check_output(["git", "--no-optional-locks", "-C", str(repo), "rev-parse", "HEAD"], timeout=30).decode().strip() == args.expected_head,
            "Actual subject Git head differs")
    inputs_path = tool / "ack425-inputs.json"
    require(sha(inputs_path) == args.inputs_sha256.upper(), "Reviewed425 inputs differ")
    inputs = json.loads(inputs_path.read_bytes())
    require(inputs["OtpPin"] == PIN and inputs["OutcomeRows"] == 150, "Reviewed425 scope differs")
    source_before = []
    for row in inputs["Files"]:
        path = (repo / row["Path"]).resolve()
        require(path.is_relative_to(repo) and path.is_file() and not path.is_symlink(), "Invalid source input")
        require(path.stat().st_size == row["Bytes"] and sha(path) == row["Sha256"], "Source input differs " + row["Path"])
        source_before.append(dict(Path=row["Path"], Bytes=path.stat().st_size, Sha256=sha(path)))
    write(out / "source-before.json", source_before)
    require(args.collector.is_file(), "Actual built permanent collector required")
    dll = args.collector.resolve()
    binaries_before = [dict(Path=str(path), Bytes=path.stat().st_size, Sha256=sha(path))
        for path in sorted(dll.parent.glob("*.dll"))]
    require(any(Path(row["Path"]) == dll for row in binaries_before), "Collector association absent")
    write(out / "binaries-before.json", binaries_before)
    provider = load("ack425_provider", repo / "tools/Runtime/Erlang.OwnedGuest506Observations/collect.py")
    source = load("ack425_source", repo / "tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py")
    environment = source.sanitized_environment(os.environ)
    environment.update(ERLC_USE_SERVER="false", PYTHONDONTWRITEBYTECODE="1", DOTNET_ROLL_FORWARD="Disable")
    statuses, errors = [], []
    after_provider = None
    before_provider = None

    def execute(name, command, cwd):
        directory = out / name
        directory.mkdir()
        passed = False
        failure = None
        write(directory / "launch.json", dict(Command=command, WorkingDirectory=str(cwd),
            DeadlineSeconds=TIMEOUT, SubjectHead=args.expected_head, UnpublishedSourceTreeNoCommitClaim=local_rows is not None, SourceInputsSha256=sha(inputs_path),
            ManagedCollectorSha256=sha(dll), FrameworkVersion=FRAMEWORK))
        try:
            if os.name == "nt":
                owned = load("ack425_windows_owned", repo / provider.HELPERS / "owned.py")
                receipt = owned.owned(command, directory, "process", TIMEOUT, environment, str(cwd))
                require(receipt["Settled"] and not receipt["TimedOut"] and receipt["ExitCode"] == 0 and not receipt["Failures"], "Owned Windows process failed")
            else:
                owned = load("ack425_linux_owned", repo / "tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py")
                process = None
                try:
                    process = owned.OwnedProcess(command, cwd, environment, directory / "process")
                    process.wait(time.monotonic() + TIMEOUT, lambda: False)
                finally:
                    if process is not None:
                        process.close()
                receipt_path = directory / "process.process.json"
                require(receipt_path.is_file(), "Missing actual owned process receipt")
                source.require_normal_receipt(json.loads(receipt_path.read_bytes()))
            passed = True
        except Exception as error:
            failure = repr(error)
        row = dict(Name=name, Passed=passed, Failure=failure)
        write(directory / "completion.json", row)
        statuses.append(row)
        return passed

    try:
        installed, erl, before_provider, after_provider = provider.provider(args, repo)
        write(out / "provider-before.json", before_provider)
        erlc = installed / "bin" / ("erlc.exe" if os.name == "nt" else "erlc")
        require(erlc.is_file(), "Admitted original compiler missing")
        original = out / "original425"
        original.mkdir()
        observer_source = original / "source"
        observer_source.mkdir()
        observer_ebin = original / "observer-ebin"
        observer_ebin.mkdir()
        facet_source = original / "facet-source"
        facet_source.mkdir()
        facet_ebin = original / "facet-ebin"
        facet_ebin.mkdir()
        for name, target in [("beam_internal_process425.erl", observer_source), ("erts_internal.erl", facet_source)]:
            origin = repo / FIXTURES / name
            shutil.copyfile(origin, target / name)
            require(sha(origin) == sha(target / name), "Unchanged original source copy differs")
        # Same debug_info/deterministic flags and original source filenames.
        worker_ok = execute("compile-observer425", [str(erlc), "+debug_info", "+deterministic", "-o", str(observer_ebin), str(observer_source / "beam_internal_process425.erl")], original)
        facet_ok = execute("compile-facet425", [str(erlc), "+debug_info", "+deterministic", "-o", str(facet_ebin), str(facet_source / "erts_internal.erl")], original)
        observed = False
        if worker_ok:
            expression = "beam_internal_process425:capture([" + json.dumps(original.as_posix()) + "])."
            # Facet directory is NEVER put on OTP's code path. Original uses
            # its genuine built-in original erts_internal implementation.
            observed = execute("original-values-stacks425", [str(erl), "-noshell", "-noinput", "-pa", str(observer_ebin), "-eval", expression], original)
            observed = observed and all((original / name).is_file() for name in ("values.etf", "stacks.etf"))
        else:
            errors.append("Observer compilation failed; dependent original execution not launched")
        managed_results = {}
        if worker_ok and facet_ok:
            managed_inputs = out / "managed-inputs425"
            managed_inputs.mkdir()
            rows = []
            for name, source_path in [("beam_internal_process425.beam", observer_ebin / "beam_internal_process425.beam"), ("erts_internal.beam", facet_ebin / "erts_internal.beam")]:
                shutil.copyfile(source_path, managed_inputs / name)
                rows.append(dict(Name=name, Bytes=source_path.stat().st_size, Sha256=sha(source_path)))
            write(managed_inputs / "managed-inputs.json", dict(OtpPin=PIN, Files=rows, CompileOptions=["+debug_info", "+deterministic"]))
            seal = sha(managed_inputs / "managed-inputs.json")
            dotnet = shutil.which(args.dotnet)
            require(dotnet is not None, "dotnet executable absent")
            for profile in ("ordinary", "private-owned"):
                destination = out / ("managed425-" + profile)
                managed_results[profile] = execute("replay425-" + profile,
                    [str(Path(dotnet).resolve()), "--fx-version", FRAMEWORK, str(dll), "--ack-original425",
                     str(managed_inputs), str(destination), profile, seal], repo)
        else:
            errors.append("Fresh BEAM preparation incomplete; dependent managed replay not launched")
        comparisons = []
        for profile in ("ordinary", "private-owned"):
            for name in ("values.etf", "stacks.etf"):
                expected = original / name
                actual = out / ("managed425-" + profile) / name
                valid = observed and managed_results.get(profile, False) and expected.is_file() and actual.is_file()
                row = dict(Profile=profile, File=name, Passed=valid and expected.read_bytes() == actual.read_bytes(),
                    OriginalSha256=sha(expected) if expected.is_file() else None,
                    ManagedSha256=sha(actual) if actual.is_file() else None, Projection=False, WholeRawEtf=True)
                comparisons.append(row)
        write(out / "whole425-comparisons.json", comparisons)
        require(all(row["Passed"] for row in comparisons), "Fresh full425 outcomes/stacks differ or were not executed")
    except Exception as error:
        errors.append(repr(error))
    finally:
        if after_provider is not None:
            try:
                after = after_provider()
                write(out / "provider-after.json", after)
                require(after == before_provider["Inventory"], "Original provider changed")
            except Exception as error:
                errors.append("Provider cleanup/integrity: " + repr(error))
        try:
            after = [dict(Path=row["Path"], Bytes=(repo / row["Path"]).stat().st_size, Sha256=sha(repo / row["Path"])) for row in source_before]
            write(out / "source-after.json", after)
            require(after == source_before, "Source inputs changed")
            after_dll = [dict(Path=row["Path"], Bytes=Path(row["Path"]).stat().st_size, Sha256=sha(row["Path"])) for row in binaries_before]
            write(out / "binaries-after.json", after_dll)
            require(after_dll == binaries_before, "Managed binaries changed")
            if local_rows is not None:
                local_after = [dict(Path=row["Path"], Bytes=(repo / row["Path"]).stat().st_size, Sha256=sha(repo / row["Path"])) for row in local_rows]
                write(out / "local-source-after.json", local_after)
                require(local_after == local_rows, "Unpublished local source tree changed")
            else:
                require(subprocess.check_output(["git", "--no-optional-locks", "-C", str(repo), "rev-parse", "HEAD"], timeout=30).decode().strip() == args.expected_head,
                    "Subject Git head changed during observations")
        except Exception as error:
            errors.append("Final integrity: " + repr(error))
        passed = not errors and bool(statuses) and all(row["Passed"] for row in statuses)
        write(out / "completion.json", dict(Passed=passed, OtpPin=PIN, SubjectHead=args.expected_head,
            UnpublishedSourceTreeNoCommitClaim=local_rows is not None, LocalSourceManifestSha256=args.local_source_manifest_sha256,
            OutcomeRowsPerProfile=150, Profiles=["ordinary", "private-owned"], WholeRawEtf=True, Projection=False,
            HistoricalExpectedChanged=False, NativeResourceLifetimeClaim=False, Results=statuses, Errors=errors))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
