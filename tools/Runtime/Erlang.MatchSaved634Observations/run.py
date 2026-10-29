"""Fresh pinned OTP634 constants versus exact internal managed protocol; no builds or normalization."""
import argparse, hashlib, importlib.util, json, os, re, shutil, sys, time
from pathlib import Path
sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
CASE = "runtime/match634/internal-native-constant-original-protocol"
SCHEMA = "match634/constant-table-v1"
COUNT = 10


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def need(value, message):
    if not value:
        raise RuntimeError(message)


def save(path, value):
    with path.open("x", encoding="utf-8") as stream:
        json.dump(value, stream, indent=2)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def erlstr(value):
    return '"' + str(value).replace("\\", "/").replace('"', '\\"') + '"'


def preflight(repo, tool):
    plan = json.loads((tool / "inputs.json").read_bytes())
    need(plan["SourcePin"] == PIN and plan["ExpectedEntries"] == COUNT, "Wrong source pin/protocol count")
    for row in plan["Inputs"]:
        need(sha(repo / row["Path"]) == row["Sha256"], "Reviewed source/helper differs: " + row["Path"])
    fixture = tool / "fixtures/match_saved634.erl"
    need(sha(fixture) == plan["OriginalFixtureSha256"], "Original fixture changed")
    names = (repo / "tests/Erlang.Tests/Runtime/Storage/MatchSpecs/MatchSaved634Names.cs").read_text()
    for key, expected in [("ProtocolSchema", SCHEMA), ("OriginalPin", PIN), ("OriginalObserverSha256", sha(fixture))]:
        found = re.search(r"const string " + key + r' = "([^"]+)"', names)
        need(found is not None and found.group(1) == expected, "Managed declared protocol differs: " + key)
    return plan


def original(args, repo, tool, output, owned):
    folder = output / "original"
    folder.mkdir()
    errors, passed, inventory, after = [], False, None, None
    executables = {}
    try:
        provider = load("match634_provider", repo / "tools/Runtime/Erlang.OwnedGuest506Observations/collect.py")
        args.build_receipt = None
        args.otp_source = None
        installed, erl, inventory, after = provider.provider(args, repo)
        save(folder / "provider-before.json", inventory)
        erlc = installed / "bin/erlc.exe"
        need(erlc.is_file(), "Admitted compiler is absent")
        executables = {"Compiler": {"Path": str(erlc), "Sha256": sha(erlc)}, "Runtime": {"Path": str(erl), "Sha256": sha(erl)}}
        shutil.copyfile(tool / "fixtures/match_saved634.erl", folder / "match_saved634.erl")
        env = dict(os.environ, PYTHONDONTWRITEBYTECODE="1", ERL_CRASH_DUMP=str(folder / "erl_crash.dump"))
        for key in ["ERL_FLAGS", "ERL_AFLAGS", "ERL_ZFLAGS", "ERL_LIBS", "ERL_COMPILER_OPTIONS", "ERLC_EMULATOR", "ERLC_USE_SERVER", "ERLC_SERVER_ID", "ERLC_CONFIGURATION", "ERL_ROOTDIR"]:
            env.pop(key, None)
        env["ERL_FLAGS"] = "+S 1:1 +A 1"
        # The original VM invocation depends on successful genuine compilation.
        owned.owned([str(erlc), "+debug_info", "-o", str(folder), str(folder / "match_saved634.erl")], folder, "compile", 120, env, str(folder))
        expr = ('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
                'Expected=filename:absname(' + erlstr(installed) + '),Expected=filename:absname(code:root_dir()),'
                '8=erlang:system_info(wordsize),1=erlang:system_info(schedulers_online),false=erlang:system_info(debug_compiled),'
                'Endian=case <<1:16/native>> of <<1,0>>->little;<<0,1>>->big end,little=Endian,'
                'W("profile.etf",{erlang:system_info(wordsize),Endian,erlang:system_info(schedulers),erlang:system_info(schedulers_online),erlang:system_info(debug_compiled),erlang:system_info(otp_release)}),'
                'match_saved634:module_info(),P=filename:absname("match_saved634.beam"),P=filename:absname(code:which(match_saved634)),'
                '{ok,{match_saved634,D}}=beam_lib:md5(P),D=match_saved634:module_info(md5),'
                'W("observer-owner.etf",{P,D,match_saved634:module_info(compile)}),'
                '[begin M:module_info(),W(atom_to_list(M)++"-owner.etf",{M,code:which(M),M:module_info(md5),M:module_info(compile)}) end||M<-[ets,lists]],'
                'Outcome=match_saved634:run(),W("observed.etf",Outcome),io:format("~tp~n",[Outcome]),'
                'case Outcome of {ok,Rows} when is_list(Rows),length(Rows)=:=10->halt(0);_->halt(1) end.')
        (folder / "invocation.eval").write_text(expr, encoding="utf-8")
        owned.owned([str(erl), "+S", "1:1", "+A", "1", "-noshell", "-noinput", "-boot", "no_dot_erlang", "-pa", str(folder), "-eval", expr], folder, "original", 60, env, str(folder))
        for name in ["observed.etf", "profile.etf", "observer-owner.etf", "ets-owner.etf", "lists-owner.etf", "match_saved634.beam"]:
            need((folder / name).is_file(), "Actual original output absent: " + name)
        passed = True
    except Exception as error:
        errors.append(repr(error))
    finally:
        if after is not None:
            try:
                current = after()
                save(folder / "provider-after.json", {"ProviderEqual": current == inventory["Inventory"], "Inventory": current})
                need(current == inventory["Inventory"], "Original provider changed")
            except Exception as error:
                errors.append("provider-postflight: " + repr(error))
        try:
            if (folder / "match_saved634.erl").exists():
                need(sha(folder / "match_saved634.erl") == sha(tool / "fixtures/match_saved634.erl"), "Copied fixture changed")
            for item in executables.values():
                need(sha(item["Path"]) == item["Sha256"], "Original executable changed")
        except Exception as error:
            errors.append("source-postflight: " + repr(error))
        result = {"Passed": passed and not errors, "Errors": errors, "SourcePin": PIN,
                  "FreshOriginalCompilation": (folder / "compile.live.json").exists(),
                  "FreshOriginalExecution": (folder / "original.live.json").exists(),
                  "Executables": executables, "ExpectedEntries": COUNT, "OriginalPhysicalDestructorsInstrumented": False,
                  "RawEtfRewritten": False, "FrameProjection": False,
                  "Files": {p.name: sha(p) for p in folder.iterdir() if p.is_file()}}
        save(folder / "completion.json", result)
    return result


def managed(args, repo, tool, output, owned):
    folder = output / "managed"
    folder.mkdir()
    errors, hashes = [], {}
    host = repo / "tests/Erlang.Tests/bin" / args.configuration / "net10.0/Erlang.Tests.dll"
    runtime = host.with_name("Erlang.Runtime.dll")
    base = repo / "artifacts/match634-components"
    existing = set(base.iterdir()) if base.exists() else set()
    started = time.time_ns()
    frame = {"Passed": False, "Errors": errors, "Case": CASE, "Configuration": args.configuration, "StartedUnixNs": started}
    try:
        for name, path in [("Host", host), ("Runtime", runtime)]:
            need(path.is_file(), "Managed assembly is absent: " + name)
            hashes[name] = sha(path)
        frame["DllBefore"] = dict(hashes)
        owned.owned(["dotnet", "exec", "--fx-version", args.framework_version, str(host), "--case", CASE, "--report", str(folder / "report.json")], folder, "managed", 90, dict(os.environ, DOTNET_PROCESSOR_COUNT="2", PYTHONDONTWRITEBYTECODE="1"), str(repo))
    except Exception as error:
        errors.append(repr(error))
    finally:
        completed = time.time_ns()
        frame["CompletedUnixNs"] = completed
        # Collect independent postconditions independently; an earlier failure must
        # not hide the actual raw files or the real process/cleanup receipt.
        try:
            for name, path in [("Host", host), ("Runtime", runtime)]:
                need(name in hashes and sha(path) == hashes[name], "Managed assembly changed: " + name)
        except Exception as error:
            errors.append("dll-postflight: " + repr(error))
        try:
            report = json.loads((folder / "report.json").read_bytes())
            need(report["Passed"] == 1 and report["Failed"] == 0 and report["Partial"] and report["SelectedCount"] == 1 and len(report["Tests"]) == 1 and report["Tests"][0]["Name"] == CASE and report["Tests"][0]["Status"] == "Passed", "Exact managed protocol case did not pass")
        except Exception as error:
            errors.append("case-postflight: " + repr(error))
        try:
            receipt = json.loads((folder / "managed.exit.json").read_bytes())
            need(receipt["Settled"] and receipt["ExitCode"] == 0 and not receipt["TimedOut"] and not receipt["Failures"], "Managed child did not settle successfully")
        except Exception as error:
            errors.append("process-postflight: " + repr(error))
        try:
            added = set(base.iterdir()) - existing
            need(len(added) == 1, "Expected exactly one fresh protocol output directory")
            fresh = added.pop()
            need(fresh.is_dir(), "Protocol output is not a directory")
            frame["FreshOutput"] = str(fresh)
            for path in fresh.iterdir():
                if path.is_file():
                    need(started <= path.stat().st_mtime_ns <= completed, "Protocol file outside owned child window: " + path.name)
                    shutil.copyfile(path, folder / path.name)
            proof = json.loads((folder / "receipt.json").read_bytes())
            frame["ActualCaptureReceipt"] = proof
            need(proof["Passed"] and proof["Schema"] == SCHEMA and proof["ExpectedEntries"] == COUNT, "Protocol capture receipt failed")
            need(proof["RuntimeSha256"] == hashes.get("Runtime") and proof["HostSha256"] == hashes.get("Host"), "Actual captured assembly identity differs")
            need(proof["RuntimeConfiguration"] == args.configuration and proof["HostConfiguration"] == args.configuration, "Actual assembly configurations differ")
            need(proof["PointerBytes"] == 8 and proof["ProcessArchitecture"] == "X64" and proof["LittleEndian"], "Unsupported actual managed physical profile")
            need(proof["OriginalSourcePin"] == PIN and proof["OriginalFixtureSha256"] == sha(tool / "fixtures/match_saved634.erl"), "Original fixture provenance differs")
            need(proof["CompiledPrograms"] == COUNT and proof["CompletedRuns"] == COUNT and proof["PhysicallyFreedPrograms"] == COUNT, "Actual program execution/free counts differ")
            need(proof["ActorDisposeCompleted"] and proof["RuntimeDisposeCompleted"] and proof["NativeProgramCountRestored"] and proof["NativeProgramCountBefore"] == 0 and proof["NativeProgramCountAfter"] == 0, "Actual cleanup/native program baseline did not settle")
            need(proof["WholeOutcomeCaptured"] and proof["InternalComponent"] and not proof["PublicProducerRedirected"] and not proof["RawEtfRewritten"] and not proof["FrameProjection"] and not proof["GoldenCompared"], "Wrong capture route or transformed result")
            need(not proof["ExceptionalFrameCompatibilityClaimed"], "Unexpected exceptional stack claim")
            need(sha(folder / "observed.etf") == proof["ActualSha256"], "Raw managed Outcome identity differs")
            frame["ProtectedFiles"] = {p.name: sha(p) for p in folder.iterdir() if p.is_file()}
        except Exception as error:
            errors.append("capture-postflight: " + repr(error))
        frame["Passed"] = not errors
        save(folder / "frame.json", frame)
    return frame


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--official-admission", type=Path, required=True)
    parser.add_argument("--admission-sha256", required=True)
    parser.add_argument("--configuration", choices=["Debug", "Release"], default="Debug")
    parser.add_argument("--framework-version", default="10.0.11")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    repo, tool, output = args.repository.resolve(), Path(__file__).resolve().parent, args.output.resolve()
    need(os.name == "nt" and output.is_relative_to(repo / "artifacts") and not output.exists(), "Fresh confined Windows-profile artifact output required")
    preflight(repo, tool)
    seals = {"Inputs": sha(tool / "inputs.json"), "Collector": sha(Path(__file__))}
    output.mkdir(parents=True)
    owned = load("match634_owned", repo / "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py")
    results = {}
    # Routes are independent. Compiler failure skips ONLY the dependent original
    # run; exact managed execution still runs and retains its own result/cleanup.
    for name, action in [("original", original), ("managed", managed)]:
        try:
            results[name] = action(args, repo, tool, output, owned)
        except Exception as error:
            results[name] = {"Passed": False, "Errors": ["unexpected-route-error: " + repr(error)]}
            save(output / (name + "-unexpected.json"), results[name])
    equal, count, errors = False, None, []
    if results["original"]["Passed"] and results["managed"]["Passed"]:
        left, right = (output / "original/observed.etf").read_bytes(), (output / "managed/observed.etf").read_bytes()
        equal, count = left == right, len(left)
        if not equal:
            errors.append("Complete original/internal managed success Outcome bytes differ")
    else:
        errors.append("Comparison skipped because an independent prerequisite route failed")
    try:
        preflight(repo, tool)
        need(seals["Inputs"] == sha(tool / "inputs.json") and seals["Collector"] == sha(Path(__file__)), "Collector/source-plan changed")
    except Exception as error:
        errors.append("final-source-postflight: " + repr(error))
    result = {"Passed": results["original"]["Passed"] and results["managed"]["Passed"] and equal and not errors,
              "OriginalPassed": results["original"]["Passed"], "ManagedPassed": results["managed"]["Passed"],
              "WholeRawEqual": equal, "ActualOutcomeBytes": count, "ExpectedProtocolEntries": COUNT,
              "Schema": SCHEMA, "SourcePin": PIN, "Errors": errors, "SourceSeals": seals,
              "InternalManagedRouteOnly": True, "PublicProducerRedirects": 0,
              "ExceptionalFrameCompatibilityClaimed": False, "Projection": False, "GoldenRewritten": False,
              "ApiDelta": 0, "FullContractDelta": 0,
              "Files": {p.relative_to(output).as_posix(): sha(p) for p in output.rglob("*") if p.is_file()}}
    save(output / "completion.json", result)
    return 0 if result["Passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
