"""Root-owned focused execution proposal. Source author did not build or run it."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def write(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def owned(command, directory, name, timeout, environment, cwd):
    stdout_path = directory / (name + ".stdout.log")
    stderr_path = directory / (name + ".stderr.log")
    started = time.monotonic()
    process = None
    timed_out = False
    failures = []
    with stdout_path.open("xb") as stdout, stderr_path.open("xb") as stderr:
        try:
            process = subprocess.Popen(command, cwd=cwd, env=environment, stdout=stdout, stderr=stderr)
            write(directory / (name + ".live.json"), {"Pid": process.pid, "Command": command, "StartedUnixSeconds": time.time()})
            try:
                process.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                timed_out = True
        except Exception as error:
            failures.append(repr(error))
        finally:
            if process is not None and process.poll() is None:
                try:
                    if os.name == "nt":
                        killed = subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True, timeout=15)
                        (directory / (name + ".kill.stdout.log")).write_bytes(killed.stdout)
                        (directory / (name + ".kill.stderr.log")).write_bytes(killed.stderr)
                        if killed.returncode != 0 and process.poll() is None:
                            failures.append("Owned process-tree termination failed: " + str(killed.returncode))
                    else:
                        process.kill()
                    process.wait(timeout=15)
                except Exception as error:
                    failures.append(repr(error))
    receipt = {"Command": command, "Pid": None if process is None else process.pid,
        "ElapsedSeconds": time.monotonic() - started, "TimedOut": timed_out,
        "Settled": process is not None and process.poll() is not None,
        "ExitCode": None if process is None else process.poll(), "Failures": failures,
        "StdoutSha256": digest(stdout_path), "StderrSha256": digest(stderr_path)}
    write(directory / (name + ".exit.json"), receipt)
    if not receipt["Settled"] or timed_out or failures or receipt["ExitCode"] != 0:
        raise RuntimeError("Owned stage failed; all available raw evidence retained: " + name)
    return receipt


def verify(candidate, rows):
    for row in rows:
        path = candidate / row["Path"]
        if path.stat().st_size != row["Bytes"] or digest(path) != row["Sha256"]:
            raise RuntimeError("Frozen input drift: " + row["Path"])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dotnet", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--approved-freeze-sha256", required=True)
    args = parser.parse_args()
    preparation = Path(__file__).resolve().parent
    repo = preparation.parent.parent
    source_path = preparation / "styled-source-freeze.json"
    if digest(source_path) != args.approved_freeze_sha256.upper():
        raise RuntimeError("Root-approved styled freeze differs")
    source = json.loads(source_path.read_bytes())
    candidate = repo / source["CandidateRelativePath"]
    if digest(Path(__file__)) != source["ExecutionHelperSha256"]:
        raise RuntimeError("Root execution helper drift")
    closure_path = preparation / "source-resource-closure.json"
    if digest(closure_path) != source["SourceClosureSha256"]:
        raise RuntimeError("Closure authority drift")
    verify(candidate, json.loads(closure_path.read_bytes())["Files"])
    verify(candidate, source["PackageReadinessInputs"])
    targets_path = preparation / "focus-targets.json"
    if digest(targets_path) != source["TargetManifestSha256"]:
        raise RuntimeError("Target authority drift")
    targets = json.loads(targets_path.read_bytes())["Targets"]
    if len(targets) != 24 or len(set(targets)) != 24:
        raise RuntimeError("Expected exactly24 distinct targets")
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=False)
    write(output / "invocation.json", {"ApprovedFreezeSha256": digest(source_path), "Targets": targets,
        "Candidate": str(candidate), "SourceClosureSha256": digest(closure_path), "ReadinessExcluded": True,
        "LedgerFrozenAndIntentionallyStaleForModifiedSources": True})
    environment = dict(os.environ)
    environment.update(DOTNET_PROCESSOR_COUNT="2", MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
    project = candidate / "tests/Erlang.Priority351.Focus/Erlang.Priority351.Focus.csproj"
    owned([args.dotnet, "build", str(project), "--disable-build-servers", "-m:1", "-nr:false"], output, "build", 240, environment, candidate)
    dll = project.parent / "bin/Debug/net10.0/Priority351Focus.dll"
    dlls = [{"Path": str(path), "Sha256": digest(path)} for path in sorted(dll.parent.glob("*.dll"))]
    write(output / "built-dlls.json", {"Files": dlls})
    owned([args.dotnet, str(dll), "--parse", str(output / "parse.json")], output, "parse", 45, environment, candidate)
    parsed = json.loads((output / "parse.json").read_bytes())
    if not parsed["Passed"] or parsed["Name"] != "--parse" or parsed["Failure"] is not None or len(parsed["Sources"]) != 10:
        raise RuntimeError("ParseAll10 failed or empty")
    passed = []
    for index, name in enumerate(targets):
        directory = output / ("target-" + str(index).zfill(2))
        directory.mkdir()
        outcome = directory / "outcome.json"
        owned([args.dotnet, str(dll), "--target", name, str(outcome)], directory, "process", 90, environment, candidate)
        report = json.loads(outcome.read_bytes())
        if not report["Passed"] or report["Name"] != name or report["Failure"] is not None or report["Sources"] != parsed["Sources"]:
            raise RuntimeError("Missing/mismatched actual target outcome: " + name)
        expected_stdout = "PASS " + name
        if (directory / "process.stdout.log").read_text().strip() != expected_stdout:
            raise RuntimeError("Target execution marker missing: " + name)
        for assembly in report["LoadedAssemblies"]:
            path = Path(assembly["Path"])
            if not path.is_relative_to(dll.parent) or digest(path) != assembly["Sha256"]:
                raise RuntimeError("Loaded DLL authority mismatch")
        if len(report["LoadedAssemblies"]) != 8:
            raise RuntimeError("Required8 actual loaded assemblies missing")
        if report["Observation"] is not None:
            actual = report["Observation"]
            if not actual["Passed"] or actual["Name"] != name or actual["Actual"] != actual["Expected"]:
                raise RuntimeError("Compiled actual/expected mismatch")
        passed.append({"Name": name, "OutcomeSha256": digest(outcome)})
    verify(candidate, json.loads(closure_path.read_bytes())["Files"])
    verify(candidate, source["PackageReadinessInputs"])
    write(output / "completion.json", {"ManagedPassed": len(passed), "ParseAll": 10, "Targets": passed,
        "NativeExecuted": False, "CompatibilityStatusesPromoted": False, "FullValidationExecuted": False,
        "ReadinessExecuted": False, "ApprovedSourceFreezeSha256": digest(source_path)})


if __name__ == "__main__":
    main()
