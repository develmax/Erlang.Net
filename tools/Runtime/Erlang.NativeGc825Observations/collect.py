"""Finite native GC roots/original820 batch; real projects and owned Linux processes."""
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
FRAMEWORK = "10.0.11"
CONTROL_SECONDS = 45
MODES = (
    "literal800", "lifetime801", "empty803", "binary805", "halt811",
    "tokenMutable550", "tokenLiteral814", "trace817", "prefix817", "priority817",
    "onheap817", "preparedNative810", "preparedBuilder810", "preparedAdmission810",
    "sourceLiteral819", "sourceParser820", "sourceDeep820", "suspensionRoot821",
    "resource524Guest", "resource524Image", "nativeBinaryInfo822", "binaryOwner823",
    "sourceObserver820", "controlRoots826", "hostCopy827", "sourceLocation146",
    "functionUnique212", "capacity273",
)


ADDITIONAL_CLI_ROWS = ("ack425-ordinary", "ack425-private-owned", "ack173-ordinary", "ack173-private-owned")


def require(value, message):
    if not value:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def write(path, value):
    Path(path).write_bytes((json.dumps(value, indent=2) + "\n").encode())


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def git(repo, *arguments):
    return subprocess.check_output(["git", "--no-optional-locks", "-C", str(repo), *arguments], timeout=30)


def frozen_sources(repo):
    rows = []
    for name in git(repo, "ls-files", "-z").decode().split("\0"):
        if not name:
            continue
        path = repo / name
        if path.suffix in {".cs", ".csproj", ".props", ".targets", ".sln", ".slnx"} or name in {
            "global.json", ".gitattributes", ".github/workflows/native-gc825.yml"
        } or name.startswith((TOOL + "/", PROJECT + "/Resources/", "tests/Erlang.Tests/Fixtures/InternalProcessOriginal425/")):
            require(path.is_file() and not path.is_symlink(), "Invalid tracked source " + name)
            rows.append(dict(Path=name, Bytes=path.stat().st_size, Sha256=sha(path)))
    require(rows, "Empty source freeze")
    return sorted(rows, key=lambda row: row["Path"])


def main():
    require(sys.platform.startswith("linux"), "This collector requires genuine Linux; no platform spoof")
    parser = argparse.ArgumentParser()
    for name in ("repository", "expected-head", "inputs-sha256", "build-receipt", "admission-sha256", "otp-source", "output"):
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    args = parser.parse_args()
    repo = Path(args.repository).resolve()
    tool = Path(__file__).resolve().parent
    output = Path(args.output).resolve()
    require(tool == repo / TOOL, "Collector must be integrated at its tracked path")
    require(output.is_relative_to(repo / "artifacts") and not output.exists(), "Fresh confined output required")
    output.mkdir(parents=True)
    statuses = []
    failures = []
    source_before = None
    provider_before = None
    provider_after = None
    try:
        require(git(repo, "rev-parse", "HEAD").decode().strip() == args.expected_head, "Subject HEAD differs")
        require(not git(repo, "status", "--porcelain", "--untracked-files=no").strip(), "Tracked source tree differs")
        inputs_path = tool / "inputs.json"
        require(sha(inputs_path) == args.inputs_sha256.upper(), "Reviewed inputs differ")
        inputs = json.loads(inputs_path.read_bytes())
        require(inputs["OtpPin"] == PIN and tuple(inputs["Modes"]) == MODES, "Reviewed scope differs")
        for row in inputs["Files"]:
            path = (repo / row["Path"]).resolve()
            require(path.is_relative_to(repo) and path.is_file() and not path.is_symlink(), "Invalid input path")
            require(sha(path) == row["Sha256"] and path.stat().st_size == row["Bytes"], "Input differs " + row["Path"])
        source_before = frozen_sources(repo)
        write(output / "source-before.json", source_before)
        source_seal = sha(output / "source-before.json")
        source = load("gc825_source_inputs", repo / "tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py")
        owned = load("gc825_owned", repo / "tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py")
        provider = load("gc825_provider", repo / "tools/Runtime/Erlang.OwnedGuest506Observations/collect.py")
        environment = source.sanitized_environment(os.environ)
        for key in tuple(environment):
            if key.startswith(("ERLANG_SOURCE820_", "ERLANG_SOURCE819_", "LITERAL815_")) or key in {
                "ERLANG_REGISTER_CAPACITY273_FIXTURES", "ERLANG_APPEND206_OUTCOME_PREFIX"
            }:
                environment.pop(key, None)
        environment.update(ERLC_USE_SERVER="false", PYTHONDONTWRITEBYTECODE="1", DOTNET_ROLL_FORWARD="Disable", DOTNET_PROCESSOR_COUNT="2",
            MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_USE_MSBUILD_SERVER="0")
        dotnet = shutil.which(args.dotnet)
        require(dotnet is not None, "dotnet executable missing")
        dotnet = str(Path(dotnet).resolve())

        def execute(name, command, cwd, timeout, env):
            directory = output / name
            directory.mkdir()
            prefix = directory / "process"
            process = None
            errors = []
            write(directory / "launch.json", dict(Arguments=command, WorkingDirectory=str(cwd), SourceFreezeSha256=source_seal,
                  SubjectHead=args.expected_head, DeadlineSeconds=timeout, FrameworkVersion=FRAMEWORK))
            try:
                process = owned.OwnedProcess(command, cwd, env, prefix)
                process.wait(time.monotonic() + timeout, lambda: False)
            except Exception as error:
                errors.append(repr(error))
            finally:
                if process is not None:
                    try:
                        process.close()
                    except Exception as error:
                        errors.append("cleanup: " + repr(error))
            receipt_path = prefix.with_suffix(".process.json")
            receipt = json.loads(receipt_path.read_bytes()) if receipt_path.is_file() else None
            if receipt is None:
                errors.append("Missing owned process settlement receipt")
            else:
                try:
                    source.require_normal_receipt(receipt)
                except Exception as error:
                    errors.append("Non-normal process settlement: " + repr(error))
            status = dict(Name=name, Passed=not errors, Executed=process is not None, Errors=errors,
                          ReceiptPath=str(receipt_path), ReceiptSha256=sha(receipt_path) if receipt_path.is_file() else None)
            for stream in ("stdout", "stderr"):
                path = prefix.with_suffix("." + stream)
                status[stream.capitalize() + "Sha256"] = sha(path) if path.is_file() else None
            write(directory / "completion.json", status)
            statuses.append(status)
            return status

        # Provider failure must not prevent independent managed build/case evidence.
        executable = None
        try:
            admission = SimpleNamespace(official_admission=None, build_receipt=Path(args.build_receipt),
                admission_sha256=args.admission_sha256, otp_source=Path(args.otp_source), expected_head=args.expected_head)
            installed, executable, provider_before, provider_after = provider.provider(admission, repo)
            write(output / "provider-before.json", provider_before)
        except Exception as error:
            failures.append("Original provider admission: " + repr(error))

        def original_batch(name, fixtures, expression, products):
            # Every batch owns independent source copies/process/settlement.
            # A compiler/worker failure cannot suppress unrelated observations.
            directory = output / name
            errors = []
            launched = False
            fixture_rows = []
            if executable is None:
                errors.append("Provider admission failed; dependent original process not launched")
            else:
                try:
                    directory.mkdir()
                    for fixture in fixtures:
                        require(fixture.is_file() and not fixture.is_symlink(), "Invalid fixture " + str(fixture))
                        shutil.copyfile(fixture, directory / fixture.name)
                        fixture_rows.append(dict(Path=str(fixture.relative_to(repo)), SourceSha256=sha(fixture),
                            CopiedSha256=sha(directory / fixture.name)))
                        require(fixture_rows[-1]["SourceSha256"] == fixture_rows[-1]["CopiedSha256"], "Fixture copy differs")
                    result = execute(name + "-process", [str(executable), "+S", "1:1", "+A", "1",
                        "-noshell", "-noinput", "-eval", expression], directory, CONTROL_SECONDS, environment)
                    launched = result["Executed"]
                    if not result["Passed"]:
                        errors.append("Original compiler/worker process or cleanup failed")
                    for product in products:
                        target = directory / product
                        if not target.is_file() or target.is_symlink():
                            errors.append("Missing actual original product " + product)
                    for row in fixture_rows:
                        if sha(directory / Path(row["Path"]).name) != row["CopiedSha256"]:
                            errors.append("Original compilation changed its source copy " + row["Path"])
                except Exception as error:
                    errors.append(repr(error))
            products_seen = []
            for product in products:
                try:
                    target = directory / product
                    if target.is_file() and not target.is_symlink():
                        products_seen.append(dict(Path=product, Bytes=target.stat().st_size, Sha256=sha(target)))
                except Exception as error:
                    errors.append("Product receipt: " + repr(error))
            passed = not errors
            status = dict(Name=name + "-preparation", Passed=passed, Executed=launched,
                Errors=errors, Fixtures=fixture_rows, Products=products_seen, SamePinnedProvider=executable is not None)
            write(output / (name + "-preparation.json"), status)
            statuses.append(status)
            if not passed:
                failures.append(name + " preparation/observation failed")

            return directory, passed

        fixtures = repo / PROJECT / "Resources/Original820"
        expression = ('C=[compile:file(F,[debug_info,return_errors,return_warnings])||F<-'
            '["source_literalpool_subject820.erl","source_literalpool_probe820.erl"]],'
            'ok=file:write_file("compile.etf",term_to_binary(C)),'
            'true=lists:all(fun({ok,_,_})->true;(_)->false end,C),'
            'V=source_literalpool_probe820:observe(),'
            'ok=file:write_file("outcome.etf",term_to_binary(V,[{minor_version,2}])),'
            'ok=file:write_file("outcome-deterministic.etf",term_to_binary(V,[deterministic,{minor_version,2}])),'
            'halt(0).')
        original, original_ok = original_batch("original820",
            [fixtures / name for name in ("source_literalpool_subject820.erl", "source_literalpool_probe820.erl")],
            expression, ("compile.etf", "outcome.etf", "outcome-deterministic.etf"))

        capacity_names = ("register_capacity273_observer", "register_capacity273_wide",
            "register_capacity273_boundary", "register_capacity273_live")
        capacity_fixtures = repo / PROJECT / "Resources/RegisterCapacity273"
        capacity_expression = ('C=[compile:file("register_capacity273_observer.erl",'
            '[no_inline,debug_info,deterministic,return_errors,return_warnings])|'
            '[compile:file(F,[from_asm,no_postopt,debug_info,deterministic,return_errors,return_warnings])||F<-'
            '["register_capacity273_wide.S","register_capacity273_boundary.S","register_capacity273_live.S"]]],'
            'ok=file:write_file("compile.etf",term_to_binary(C)),'
            'true=lists:all(fun({ok,_,_})->true;(_)->false end,C),'
            'Parent275=self(),{Pid275,Monitor275}=spawn_monitor(fun()->Parent275!{finished275,'
            '{register_capacity273_observer:run(),register_capacity273_wide:run(),'
            'register_capacity273_boundary:run(),apply(register_capacity273_live,run,lists:seq(0,63))}} end),'
            'V=receive {finished275,Value275}->receive '
            "{'DOWN',Monitor275,process,Pid275,normal}->Value275 "
            'after 10000->erlang:error(capacity_worker_down_timeout) end;'
            "{'DOWN',Monitor275,process,Pid275,Reason275}->erlang:error({capacity_worker_failed,Reason275}) "
            'after 10000->erlang:error(capacity_worker_timeout) end,'
            'ok=file:write_file("original.etf",term_to_binary(V,[{minor_version,2}])),'
            'ok=file:write_file("original-deterministic.etf",term_to_binary(V,[deterministic,{minor_version,2}])),'
            'halt(0).')
        capacity, capacity_ok = original_batch("original-capacity273",
            [capacity_fixtures / (name + (".erl" if name.endswith("observer") else ".S")) for name in capacity_names],
            capacity_expression, ("compile.etf", "original.etf", "original-deterministic.etf")
                + tuple(name + ".beam" for name in capacity_names))

        append_fixtures = repo / PROJECT / "Resources/NativeAppend206"
        append_expression = ('C=compile:file("native_append206_observer.erl",'
            '[debug_info,deterministic,return_errors,return_warnings]),'
            'ok=file:write_file("compile.etf",term_to_binary(C)),{ok,native_append206_observer,_}=C,'
            '{Pid,Ref}=spawn_monitor(native_append206_observer,worker,[self()]),'
            'V=receive {finished,Value}->Value;'
            "{'DOWN',Ref,process,Pid,Reason}->erlang:error({append_worker_failed,Reason}) "
            'after 10000->erlang:error(append_worker_timeout) end,'
            "receive {'DOWN',Ref,process,Pid,normal}->ok;"
            "{'DOWN',Ref,process,Pid,ExitReason}->erlang:error({append_worker_exit,ExitReason}) "
            'after 10000->erlang:error(append_worker_down_timeout) end,'
            '164=length(V),'
            'ok=file:write_file("original.etf",term_to_binary(V,[{minor_version,2}])),'
            'ok=file:write_file("original-deterministic.etf",term_to_binary(V,[deterministic,{minor_version,2}])),'
            'halt(0).')
        append_original, append_ok = original_batch("original-append206",
            [append_fixtures / "native_append206_observer.erl"], append_expression,
            ("compile.etf", "native_append206_observer.beam", "original.etf", "original-deterministic.etf"))

        # Fresh425 reuses its unchanged full suite and managed-only native
        # facet. Compile and observe in DIFFERENT original VMs, preserving the
        # historical worker context. Never put the facet on OTP's code path.
        process425 = repo / "tests/Erlang.Tests/Fixtures/InternalProcessOriginal425"
        compile425 = ('ok=file:make_dir("managed-facet"),'
            'C=[compile:file("beam_internal_process425.erl",[debug_info,deterministic,return_errors,return_warnings]),'
            'compile:file("erts_internal.erl",[debug_info,deterministic,return_errors,return_warnings,{outdir,"managed-facet"}])],'
            'ok=file:write_file("compile.etf",term_to_binary(C)),'
            'true=lists:all(fun({ok,_,_})->true;(_)->false end,C),halt(0).')
        original425, compiled425 = original_batch("original425-compile",
            [process425 / "beam_internal_process425.erl", process425 / "erts_internal.erl"], compile425,
            ("compile.etf", "beam_internal_process425.beam", "managed-facet/erts_internal.beam"))
        trace173_fixtures = repo / TOOL / "fixtures"
        compile173 = ('C=compile:file("ack_receive_trace173.erl",[debug_info,deterministic,return_errors,return_warnings]),'
            'ok=file:write_file("compile.etf",term_to_binary(C)),{ok,ack_receive_trace173,_}=C,halt(0).')
        original173, compiled173 = original_batch("original173-compile",
            [trace173_fixtures / "ack_receive_trace173.erl"], compile173,
            ("compile.etf", "ack_receive_trace173.beam"))

        def observe_ack_original(name, directory, module, compiled, products):
            row = dict(Name=name, Passed=False, Executed=False, SamePinnedProvider=True,
                SourceCompilationSeparateFromObservation=True, Errors=[])
            try:
                if not compiled or executable is None:
                    row["Errors"].append("Dependent fresh source compilation/provider failed; original worker not launched")
                else:
                    expression = module + ':capture([' + json.dumps(directory.as_posix()) + ']).'
                    receipt = execute(name + "-process", [str(executable), "-noshell", "-noinput",
                        "-pa", str(directory), "-eval", expression], directory, CONTROL_SECONDS, environment)
                    row["Executed"] = receipt["Executed"]
                    row["Products"] = []
                    for filename in products:
                        path = directory / filename
                        require(path.is_file() and not path.is_symlink() and path.stat().st_size > 0,
                            "Missing complete actual original ACK product " + filename)
                        row["Products"].append(dict(Path=filename, Bytes=path.stat().st_size, Sha256=sha(path)))
                    row["Passed"] = receipt["Passed"]
            except Exception as error:
                row["Errors"].append(repr(error))
            write(output / (name + ".json"), row)
            statuses.append(row)
            return row["Passed"]

        original425_ok = observe_ack_original("original425-values-stacks", original425,
            "beam_internal_process425", compiled425, ("values.etf", "stacks.etf"))
        original173_ok = observe_ack_original("original173-own-id-traces", original173,
            "ack_receive_trace173", compiled173, ("values.etf", "trace-pairs.etf", "whole.etf"))

        sdk = execute("dotnet-info", [dotnet, "--info"], repo, CONTROL_SECONDS, environment)
        runtimes = execute("dotnet-runtimes", [dotnet, "--list-runtimes"], repo, CONTROL_SECONDS, environment)
        runtime_present = runtimes["Passed"] and any(line.startswith("Microsoft.NETCore.App " + FRAMEWORK + " ")
            for line in (output / "dotnet-runtimes/process.stdout").read_text().splitlines())
        if not runtime_present:
            failures.append("Exact Microsoft.NETCore.App " + FRAMEWORK + " is required; no rolling fallback")
        build = execute("managed-build", [dotnet, "build", str(repo / PROJECT / "Erlang.NativeGc.Observations.csproj"),
            "-c", "Release", "--no-incremental", "-m:1"], repo, 600, environment)
        host = repo / PROJECT / "bin/Release/net10.0/Erlang.Tests.dll"
        binaries = []
        if build["Passed"] and host.is_file():
            binaries = [dict(Path=str(path.relative_to(repo)), Sha256=sha(path))
                for path in sorted(host.parent.iterdir()) if path.is_file() and path.suffix in (".dll", ".json", ".pdb")]
            write(output / "managed-binaries-before.json", binaries)
        else:
            failures.append("Managed build failed or actual host missing")
        build_ok = build["Passed"] and host.is_file() and runtime_present and sdk["Passed"]
        observed_path = output / "managed820.etf"
        append_prefix = output / "managed-append206"
        for mode in MODES:
            if not build_ok:
                statuses.append(dict(Name=mode, Passed=False, Executed=False, Reason="Managed build/runtime preparation failed"))
                continue
            if mode == "capacity273" and not capacity_ok:
                statuses.append(dict(Name=mode, Passed=False, Executed=False,
                    Reason="Fresh pinned capacity273 compilation/whole tuple preparation failed"))
                continue
            env = dict(environment, ERLANG_SOURCE820_FIXTURES=str(repo / PROJECT / "Resources/Original820"),
                ERLANG_SOURCE820_LIBRARY_METADATA=str(repo / PROJECT / "Resources/original-library-assets06.json"))
            if mode == "capacity273":
                env["ERLANG_REGISTER_CAPACITY273_FIXTURES"] = str(capacity)
            if mode == "controlRoots826":
                env["ERLANG_APPEND206_OUTCOME_PREFIX"] = str(append_prefix)
            if mode == "sourceObserver820":
                env["ERLANG_SOURCE820_OUTCOME"] = str(observed_path)
            try:
                execute(mode, [dotnet, "exec", "--fx-version", FRAMEWORK, str(host), mode], repo, CONTROL_SECONDS, env)
            except Exception as error:
                statuses.append(dict(Name=mode, Passed=False, Executed=None,
                    Reason="Collector receipt failure; child execution state is not accepted", Errors=[repr(error)]))
                failures.append("Managed mode receipt " + mode + ": " + repr(error))
        # Four additional named CLI rows, deliberately separate from the
        # existing28 modes. All use this SAME already-built real managed host.
        def replay_ack(cohort, compiled, original_directory, images, command):
            prepared = False
            seal = None
            inputs_directory = output / ("managed-ack-inputs-" + cohort)
            try:
                if compiled:
                    inputs_directory.mkdir()
                    rows = []
                    for filename, source_path in images:
                        require(source_path.is_file() and not source_path.is_symlink(), "Fresh ACK BEAM missing")
                        target = inputs_directory / filename
                        shutil.copyfile(source_path, target)
                        require(sha(target) == sha(source_path), "Fresh ACK BEAM copy differs")
                        rows.append(dict(Name=filename, Bytes=target.stat().st_size, Sha256=sha(target)))
                    manifest = inputs_directory / ("managed-inputs.json" if cohort == "425" else "managed-trace-inputs.json")
                    write(manifest, dict(OtpPin=PIN, Files=rows, CompileOptions=["+debug_info", "+deterministic"]))
                    seal = sha(manifest)
                    prepared = True
            except Exception as error:
                statuses.append(dict(Name="managed-ack-inputs-" + cohort, Passed=False, Executed=False, Errors=[repr(error)]))
            results = {}
            for profile in ("ordinary", "private-owned"):
                name = "ack" + cohort + "-" + profile
                destination = output / ("managed-ack" + cohort + "-" + profile)
                row = dict(Name=name, Passed=False, Executed=False)
                if not build_ok or not prepared:
                    row["Reason"] = "Dependent real managed build/runtime or fresh original BEAM preparation failed"
                    statuses.append(row)
                else:
                    try:
                        row = execute(name, [dotnet, "exec", "--fx-version", FRAMEWORK, str(host), command,
                            str(inputs_directory), str(destination), profile, seal], repo, CONTROL_SECONDS, environment)
                        report_path = destination / "managed-completion.json"
                        require(report_path.is_file() and not report_path.is_symlink(), "Managed ACK cleanup receipt absent")
                        report = json.loads(report_path.read_bytes())
                        require(report["Passed"] and report["Profile"] == profile and report["ManagedInputSha256"] == seal,
                            "Managed ACK route or real cleanup failed")
                        if cohort == "425":
                            require(report["WholeOutcomeRows"] == 150 and len(report["Captured"]) == 2,
                                "Complete425 values/full-stacks scope differs")
                        else:
                            require(report["CompleteOwnExpectedTracePairsEqual"] and report["RawActualAndExpectedPairsRetained"]
                                and not report["WholeRawTraceAcrossVmsEqualClaimed"] and not report["PosthocIdentifierNormalization"],
                                "Complete173 own-state protocol receipt differs")
                        row["ManagedCompletionSha256"] = sha(report_path)
                    except Exception as error:
                        row.update(Passed=False, Errors=row.get("Errors", []) + [repr(error)])
                        if not any(existing is row for existing in statuses):
                            statuses.append(row)
                results[profile] = (row, destination)
            return results

        ack425 = replay_ack("425", compiled425, original425,
            [("beam_internal_process425.beam", original425 / "beam_internal_process425.beam"),
             ("erts_internal.beam", original425 / "managed-facet/erts_internal.beam")], "--ack-original425")
        ack173 = replay_ack("173", compiled173, original173,
            [("ack_receive_trace173.beam", original173 / "ack_receive_trace173.beam")], "--ack-trace173")
        ack_comparisons = []
        for profile in ("ordinary", "private-owned"):
            managed, directory = ack425[profile]
            for filename in ("values.etf", "stacks.etf"):
                expected = original425 / filename
                actual = directory / filename
                row = dict(Name="whole425-" + profile + "-" + filename, Passed=False,
                    OriginalExecuted=original425_ok, ManagedExecuted=managed.get("Executed", False),
                    WholeRawEtf=True, Projection=False, FieldsDropped=False, OutcomeRowsPerProfile=150)
                try:
                    if original425_ok and managed["Passed"] and expected.is_file() and actual.is_file():
                        row.update(Passed=expected.read_bytes() == actual.read_bytes(), OriginalSha256=sha(expected),
                            ManagedSha256=sha(actual), OriginalBytes=expected.stat().st_size, ManagedBytes=actual.stat().st_size)
                    else:
                        row["Reason"] = "No successful complete fresh425 original/managed streams; comparison NOT accepted"
                except Exception as error:
                    row["Errors"] = [repr(error)]
                statuses.append(row)
                ack_comparisons.append(row)
            managed, directory = ack173[profile]
            row = dict(Name="ack173-own-id-" + profile, Passed=False, OriginalExecuted=original173_ok,
                ManagedExecuted=managed.get("Executed", False), WholeVerdictEtf=True,
                CompleteOwnExpectedTracePairsRequired=True, WholeRawTraceAcrossVmsEqualClaimed=False,
                PosthocIdentifierNormalization=False)
            try:
                expected = original173 / "values.etf"
                actual = directory / "values.etf"
                # Preserve all raw actual/expected pairs and whole records;
                # they contain EACH VM's own native identities, never normalized.
                raw = []
                for origin, root in (("original", original173), ("managed", directory)):
                    for filename in ("trace-pairs.etf", "whole.etf"):
                        path = root / filename
                        require(path.is_file() and not path.is_symlink() and path.stat().st_size > 0,
                            "Complete raw own-ID ACK trace evidence missing")
                        raw.append(dict(Origin=origin, Path=str(path.relative_to(output)), Bytes=path.stat().st_size, Sha256=sha(path)))
                row["UnmodifiedRawTraceEvidence"] = raw
                if original173_ok and managed["Passed"] and expected.is_file() and actual.is_file():
                    row.update(Passed=expected.read_bytes() == actual.read_bytes(), OriginalVerdictSha256=sha(expected),
                        ManagedVerdictSha256=sha(actual), OriginalBytes=expected.stat().st_size, ManagedBytes=actual.stat().st_size)
                else:
                    row["Reason"] = "Fresh complete own-ID protocol or managed cleanup failed; no verdict accepted"
            except Exception as error:
                row["Errors"] = [repr(error)]
            statuses.append(row)
            ack_comparisons.append(row)
        write(output / "whole425-and-ack173-comparisons.json", ack_comparisons)

        comparison = dict(Name="whole820-comparison", Passed=False, OriginalExecuted=original_ok,
                          ManagedExecuted=any(row["Name"] == "sourceObserver820" and row["Executed"] for row in statuses),
                          NormalizationPerformed=False, FieldsDropped=False)
        try:
            if original_ok and observed_path.is_file():
                expected = original / "outcome.etf"
                comparison.update(Passed=observed_path.read_bytes() == expected.read_bytes(),
                    OriginalSha256=sha(expected), ManagedSha256=sha(observed_path),
                    OriginalBytes=expected.stat().st_size, ManagedBytes=observed_path.stat().st_size,
                    OriginalDeterministicSha256=sha(original / "outcome-deterministic.etf"))
            else:
                comparison["Reason"] = "Missing successful fresh original or managed complete ETF; comparison is NOT accepted"
        except Exception as error:
            comparison.update(Passed=False, Errors=[repr(error)])
        write(output / "whole820-comparison.json", comparison)
        statuses.append(comparison)
        append_comparisons = []
        managed_append = next((row for row in statuses if row["Name"] == "controlRoots826"), {})
        for profile in ("ordinary", "private-owned"):
            for deterministic in (False, True):
                suffix = ".deterministic.etf" if deterministic else ".etf"
                observed = Path(str(append_prefix) + "." + profile + suffix)
                expected = append_original / ("original-deterministic.etf" if deterministic else "original.etf")
                row = dict(Name="whole-append206-" + profile + ("-deterministic" if deterministic else "-raw"),
                    Passed=False, OriginalExecuted=append_ok, ManagedExecuted=managed_append.get("Executed", False),
                    ManagedModePassed=managed_append.get("Passed", False), ExpectedRows=164,
                    NormalizationPerformed=False, FieldsDropped=False, FramesProjected=False)
                try:
                    if append_ok and expected.is_file() and observed.is_file():
                        equal = observed.read_bytes() == expected.read_bytes()
                        row.update(WholeBytesEqual=equal, Passed=equal and managed_append.get("Passed", False),
                            OriginalSha256=sha(expected), ManagedSha256=sha(observed),
                            OriginalBytes=expected.stat().st_size, ManagedBytes=observed.stat().st_size)
                    else:
                        row["Reason"] = "Missing successful fresh original or complete managed ETF; comparison is NOT accepted"
                except Exception as error:
                    row.update(Passed=False, Errors=[repr(error)])
                append_comparisons.append(row)
                statuses.append(row)
        write(output / "whole-append206-comparisons.json", append_comparisons)
        if binaries:
            after = [dict(Path=row["Path"], Sha256=sha(repo / row["Path"])) for row in binaries]
            write(output / "managed-binaries-after.json", after)
            require(after == binaries, "Managed binaries changed during observations")
    except Exception as error:
        failures.append(repr(error))
    finally:
        if provider_after is not None:
            try:
                after = provider_after()
                write(output / "provider-after.json", after)
                require(after == provider_before["Inventory"], "Original provider/source changed during observations")
            except Exception as error:
                failures.append("Provider closure: " + repr(error))
        if source_before is not None:
            try:
                after = frozen_sources(repo)
                write(output / "source-after.json", after)
                require(after == source_before, "Source inputs changed during observations")
                require(git(repo, "rev-parse", "HEAD").decode().strip() == args.expected_head, "Subject HEAD changed")
                require(not git(repo, "status", "--porcelain", "--untracked-files=no").strip(), "Tracked source mutation")
            except Exception as error:
                failures.append("Source closure: " + repr(error))
        by_name = {row["Name"]: row for row in statuses}
        modes_complete = all(name in by_name and by_name[name]["Passed"] for name in MODES)
        compared = by_name.get("whole820-comparison", {}).get("Passed", False)
        ack_required = all(by_name.get(name, {}).get("Passed", False) for name in ADDITIONAL_CLI_ROWS)
        ack_required = ack_required and all(by_name.get("whole425-" + profile + "-" + filename, {}).get("Passed", False)
            for profile in ("ordinary", "private-owned") for filename in ("values.etf", "stacks.etf"))
        ack_required = ack_required and all(by_name.get("ack173-own-id-" + profile, {}).get("Passed", False)
            for profile in ("ordinary", "private-owned"))
        passed = not failures and modes_complete and compared and ack_required and all(row["Passed"] for row in statuses)
        write(output / "completion.json", dict(Passed=passed, SubjectHead=args.expected_head, OtpPin=PIN,
            FrameworkVersion=FRAMEWORK, ModeCount=len(MODES), PassedModes=sum(by_name.get(name, {}).get("Passed", False) for name in MODES),
            Whole820Equal=compared,
            WholeAppend206Equal=all(by_name.get("whole-append206-" + profile + "-" + encoding, {}).get("Passed", False)
                for profile in ("ordinary", "private-owned") for encoding in ("raw", "deterministic")),
            Capacity273WholeTupleAccepted=by_name.get("original-capacity273-preparation", {}).get("Passed", False)
                and by_name.get("capacity273", {}).get("Passed", False),
            AdditionalCliRows=len(ADDITIONAL_CLI_ROWS),
            PassedAdditionalCliRows=sum(by_name.get(name, {}).get("Passed", False) for name in ADDITIONAL_CLI_ROWS),
            Whole425BothProfiles=all(by_name.get("whole425-" + profile + "-" + filename, {}).get("Passed", False)
                for profile in ("ordinary", "private-owned") for filename in ("values.etf", "stacks.etf")),
            Whole425OutcomeRowsPerProfile=150,
            Ack173OwnIdProtocolBothProfiles=all(by_name.get("ack173-own-id-" + profile, {}).get("Passed", False)
                for profile in ("ordinary", "private-owned")),
            WholeRawTraceAcrossVmsEqualClaimed=False,
            FullCompatibilityClaimed=False, PublicGcAllRootsClaimed=False,
            Results=statuses, Errors=failures))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
