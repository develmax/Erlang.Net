"""Original-only Linux init progress observation. Source-only unless --execute is explicit."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
TOOL = "tools/Native/Erlang.NifInitProgress582Observations"


def need(value, reason):
    if not value:
        raise RuntimeError(reason)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def save(path, value):
    with Path(path).open("x", encoding="utf-8") as stream:
        json.dump(value, stream, indent=2)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def snapshot(path):
    path = Path(path).resolve()
    return dict(Path=str(path), Sha256=sha(path), Bytes=path.stat().st_size)


def es(path):
    value = str(path).replace("\\", "/")
    need(all(32 <= ord(c) < 127 for c in value), "ASCII owned paths required")
    return json.dumps(value)


def guard(args):
    repo = args.repository.resolve()
    tool = Path(__file__).resolve().parent
    need(tool == repo / TOOL, "Integrated original-only tool required")
    need(sys.platform.startswith("linux") and os.uname().machine == "x86_64", "Linux x64 required")
    need(re.fullmatch("[0-9a-f]{40}", args.expected_head) is not None, "Exact subject commit required")
    head = subprocess.check_output(["git", "--no-optional-locks", "-C", str(repo), "rev-parse", "HEAD"], text=True, timeout=20).strip()
    need(head == args.expected_head, "Checkout differs from explicitly selected subject")
    need(not os.environ.get("GITHUB_SHA") or os.environ["GITHUB_SHA"] == head, "GitHub subject differs")
    plan_path = tool / "inputs.json"
    need(sha(plan_path) == args.plan_sha256.upper(), "Source-only plan differs")
    plan = json.loads(plan_path.read_bytes())
    need(plan["Pin"] == PIN and plan["OriginalOnly"] is True, "Original-only pin/scope required")
    rows = plan["Inputs"]
    need(rows and len(rows) == len({(r["Scope"], r["Path"]) for r in rows}), "Nonempty duplicate-free inputs required")
    for row in rows:
        root = tool if row["Scope"] == "Tool" else repo
        path = (root / row["Path"]).resolve()
        need(path.is_relative_to(root) and path.is_file() and not path.is_symlink(), "Invalid source input " + row["Path"])
        need(sha(path) == row["Sha256"] and path.stat().st_size == row["Bytes"], "Source changed " + row["Path"])
    return repo, tool, plan


def compile_expression(installed):
    return (
        'W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
        'Expected=filename:absname(' + es(installed) + '),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
        'Compile=fun({Name,Source,Defines})->R=try {ok,F}=epp:parse_file(Source,[],Defines),'
        'C=compile:forms(F,[binary,debug_info,return_errors,return_warnings]),W(Name++"-compile.etf",C),'
        '{M,B}=case C of {ok,M0,B0}->{M0,B0};{ok,M0,B0,_}->{M0,B0} end,'
        'ok=file:write_file(Name++".beam",B),{ok,M} catch C0:R0:S0->{raised,C0,R0,S0} end,{Name,R} end,'
        'Results=[Compile(S)||S<-[{"ack","init_ack582.erl",[]},'
        '{"subject-v1","init_subject582.erl",[{\'BEAM_GENERATION582\',1}]},'
        '{"subject-v2","init_subject582.erl",[{\'BEAM_GENERATION582\',2}]},'
        '{"observer","init_observer582.erl",[]}]],W("compile-receipt.etf",Results),'
        'case lists:all(fun({_,{ok,_}})->true;(_)->false end,Results) of true->halt(0);false->halt(79) end.'
    )


def capture_expression(installed, prep, images, case):
    return (
        'W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
        'Expected=filename:absname(' + es(installed) + '),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
        '{ok,Observer}=file:read_file(' + es(prep / 'observer.beam') + '),'
        '{module,init_observer582}=code:load_binary(init_observer582,"observer.beam",Observer),'
        '{ok,AckBeam}=file:read_file(' + es(prep / 'ack.beam') + '),'
        '{ok,Beam1}=file:read_file(' + es(prep / 'subject-v1.beam') + '),'
        '{ok,Beam2}=file:read_file(' + es(prep / 'subject-v2.beam') + '),'
        'Capture=try init_observer582:capture(AckBeam,Beam1,Beam2,'
        + es(images['ack'].with_suffix('')) + ',' + es(images['v1'].with_suffix('')) + ',' + es(images['v2'].with_suffix(''))
        + ') of T0->{returned,T0} catch C0:R0:S0->{raised,C0,R0,S0} end,W("observed.etf",Capture),'
        'Events=fun()->case file:read_file(' + es(case / 'events.log') + ') of {ok,D}->[L||L<-binary:split(D,<<10>>,[global]),L=/=<<>>];{error,enoent}->[] end end,'
        'Count=fun(E)->length([ok||L<-Events(),L=:=E]) end,'
        'WaitOld=fun F(0)->erlang:error({old_unload_timeout,Events()});F(N)->case Count(<<"unload_v1">>) of 1->ok;0->receive after 10->F(N-1) end;V->erlang:error({old_unload_count,V}) end end,'
        'WaitAll=fun F(0)->erlang:error({all_unload_timeout,Events()});F(N)->case {Count(<<"unload_v1">>),Count(<<"unload_v2">>)} of {1,1}->ok;{A,B} when A=<1,B=<1->receive after 10->F(N-1) end;V->erlang:error({unload_counts,V}) end end,'
        'Cleanup=try erlang:garbage_collect(),OldPurge=code:purge(init_subject582),WaitOld(1500),'
        'Current={code:delete(init_subject582),code:purge(init_subject582)},WaitAll(1500),'
        'AckCleanup={code:delete(init_ack582),code:purge(init_ack582)},'
        'ObserverCleanup={code:delete(init_observer582),code:purge(init_observer582)},'
        '{returned,{OldPurge,Current,AckCleanup,ObserverCleanup,Events()}} catch C1:R1:S1->{raised,C1,R1,S1} end,'
        'W("cleanup.etf",Cleanup),W("events.etf",Events()),'
        'case {Capture,Cleanup} of {{returned,_},{returned,_}}->halt(0);_->halt(79) end.'
    )


def execute(args, repo, tool, plan):
    need(args.otp_source and args.build_receipt and args.admission_sha256 and args.output, "Explicit original build admission and fresh output required")
    output = args.output.resolve()
    need(output.is_relative_to(repo / "artifacts/nif-init-progress582") and not output.exists(), "Fresh confined output required")
    output.mkdir(parents=True)
    result = dict(Passed=False, Status="ORIGINAL_ONLY_INIT582", OriginalOnly=True, ManagedExecuted=False,
                  N1Executed=False, ApiDelta=0, FullContractDelta=0, Errors=[], NativeImages=[], Stages=[])
    provider_after = None
    sealed = []
    try:
        provider = load("init582_provider", repo / "tools/Runtime/Erlang.OwnedGuest506Observations/collect.py")
        args.official_admission = None
        installed, erl, before, provider_after = provider.provider(args, repo)
        save(output / "provider-before.json", before)
        identity = load("init582_identity", repo / "tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py")
        owned = load("init582_owned", repo / "tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py")
        entry = load("init582_stage", repo / "tools/Native/Erlang.NifEntryOriginalObservations/common.py")
        decoder = load("init582_etf", repo / "tools/Distribution/Erlang.GuestConnectedSendObservations/passive_etf.py")
        environment = identity.sanitized_environment(os.environ)
        for key in ["ERLC_EMULATOR", "ERLC_SERVER_ID", "ERLC_CONFIGURATION", "LD_PRELOAD", "LD_AUDIT", "LD_LIBRARY_PATH", "GITHUB_TOKEN", "GH_TOKEN"]:
            environment.pop(key, None)
        environment.update(PYTHONDONTWRITEBYTECODE="1", ERLC_USE_SERVER="false", ERL_CRASH_DUMP=str(output / "erl_crash.dump"))
        def stage(argv, directory, name, timeout=90, env=None):
            receipt = entry.stage(owned, identity, [str(v) for v in argv], directory, env or environment, directory / name, timeout)
            result["Stages"].append(receipt)
            return receipt
        headers = output / "headers"
        headers.mkdir()
        header_plan = json.loads((repo / "tools/Native/Erlang.NifFixtureHost/native/source-plan.json").read_bytes())
        for row in header_plan["Headers"]:
            if not row["OutputName"].endswith(".in"):
                path = installed / "usr/include" / row["OutputName"]
                need(sha(path) == row["Sha256"], "Pinned header differs " + row["OutputName"])
                shutil.copyfile(path, headers / row["OutputName"])
        shutil.copyfile(installed / "usr/include/erl_int_sizes_config.h", headers / "erl_int_sizes_config.h")
        sizes = (headers / "erl_int_sizes_config.h").read_text()
        for name, value in dict(SIZEOF_CHAR=1, SIZEOF_SHORT=2, SIZEOF_INT=4, SIZEOF_LONG=8, SIZEOF_LONG_LONG=8, SIZEOF_VOID_P=8).items():
            need(re.search(r"^#define\s+" + name + r"\s+" + str(value) + r"\s*$", sizes, re.M), "Wrong Linux LP64 " + name)
        compiler = shutil.which("gcc")
        nm = shutil.which("nm")
        readelf = shutil.which("readelf")
        need(compiler and nm and readelf, "Existing gcc/nm/readelf required")
        sealed = [snapshot(p) for p in [*headers.iterdir(), Path(compiler).resolve(), Path(nm).resolve(), Path(readelf).resolve(), args.build_receipt.resolve()]]
        save(output / "sealed-build-inputs.json", sealed)
        stage([compiler, "--version"], output, "compiler-version")
        images = {}
        for name, source, defines in [("ack", "init_ack582.c", []), ("v1", "init_subject582.c", ["-DGENERATION582=1"]), ("v2", "init_subject582.c", ["-DGENERATION582=2"])]:
            directory = output / name
            directory.mkdir()
            try:
                path = directory / (name + ".so")
                stage([compiler, "-shared", "-fPIC", "-std=c11", "-O2", "-Wall", "-Wextra", "-Werror", "-Wno-unused-function", "-I", headers, "-I", tool / "fixtures", *defines, tool / "fixtures" / source, "-o", path], directory, "compile")
                stage([nm, "-D", "--defined-only", "--format=posix", path], directory, "defined")
                stage([nm, "-D", "--undefined-only", "--format=posix", path], directory, "undefined")
                stage([readelf, "-h", "-Ws", "-d", path], directory, "elf")
                defined = {line.split()[0].split("@")[0] for line in (directory / "defined.stdout").read_text().splitlines() if line.split()}
                need(defined == {"nif_init"}, "Unexpected fixture exports")
                raw = path.read_bytes()
                need(raw[:6] == b"\x7fELF\x02\x01" and int.from_bytes(raw[18:20], "little") == 62, "Wrong Linux ELF class/machine")
                sealed.append(snapshot(path))
                images[name] = path
                result["NativeImages"].append(dict(Id=name, Library=snapshot(path), TargetInitializationExecutedDuringBuild=False))
            except Exception as error:
                result["Errors"].append(dict(Stage="native-" + name, Error=repr(error)))
        prep = output / "beam"
        prep.mkdir()
        for path in (tool / "fixtures").glob("*.erl"):
            shutil.copyfile(path, prep / path.name)
        expression = compile_expression(installed)
        (prep / "compile.eval").write_text(expression)
        beam_ok = False
        try:
            stage([erl, "+S", "2:2", "+A", "1", "-noshell", "-noinput", "-boot", "no_dot_erlang", "-start_epmd", "false", "-eval", expression], prep, "compile", 180)
            beams = [prep / (name + ".beam") for name in ["ack", "subject-v1", "subject-v2", "observer"]]
            need(all(path.is_file() for path in beams), "Four actual BEAM images required")
            need(sha(beams[1]) != sha(beams[2]), "Subject BEAM checksums must differ")
            result["Beams"] = [snapshot(path) for path in beams]
            sealed.extend(result["Beams"])
            beam_ok = True
        except Exception as error:
            result["Errors"].append(dict(Stage="beam-preparation", Error=repr(error)))
        if len(images) != 3 or not beam_ok:
            result["SkippedDependentCapture"] = "Genuine native/BEAM preparation failed; no target NIF initialized"
        else:
            need(len({str(p.resolve()) for p in images.values()}) == 3, "Distinct native paths required")
            case = output / "positive-ack"
            case.mkdir()
            env = dict(environment)
            for key, name in [("ERLANG_INIT582_LOG", "events.log"), ("ERLANG_INIT582_ACK", "ack"), ("ERLANG_INIT582_READY", "ready")]:
                path = case / name
                need(not path.exists(), "No stale fixture markers")
                env[key] = str(path)
            env["ERL_CRASH_DUMP"] = str(case / "erl_crash.dump")
            expression = capture_expression(installed, prep, images, case)
            (case / "invocation.eval").write_text(expression)
            try:
                stage([erl, "+S", "2:2", "+A", "1", "-noshell", "-noinput", "-boot", "no_dot_erlang", "-start_epmd", "false", "-eval", expression], case, "original", 90, env)
            except Exception as error:
                result["Errors"].append(dict(Stage="original-capture-or-cleanup", Error=repr(error)))
            observed_path = case / "observed.etf"
            cleanup_path = case / "cleanup.etf"
            if observed_path.is_file() and cleanup_path.is_file():
                observed = decoder.decode_exact(observed_path.read_bytes())
                cleanup = decoder.decode_exact(cleanup_path.read_bytes())
                (case / "observed-decoded.txt").write_text(repr(observed))
                (case / "cleanup-decoded.txt").write_text(repr(cleanup))
                atom = lambda v: ("atom", v)
                integer = lambda v: ("integer", v)
                tup = lambda *v: ("tuple", tuple(v))
                value = lambda v: tup(atom("value"), v)
                expected = tup(atom("returned"), tup(value(atom("ok")), value(atom("ok")), value(tup(integer(1), integer(0))), value(atom("ok")), value(tup(integer(2), integer(0))), value(tup(integer(1), integer(0))), value(atom("ok")), atom("normal"), integer(2)))
                need(observed == expected, "Whole nine-field original observation differs; raw result and complete raised stacks retained")
                need(cleanup[0] == "tuple" and cleanup[1][0] == atom("returned"), "Actual cleanup did not complete")
                events = (case / "events.log").read_text().splitlines()
                for name, count in [("load_accepted", 2), ("constructor_enter", 1), ("ack_attempt", 1), ("ack_written", 1), ("constructor_ack_seen", 1), ("upgrade_enter", 1), ("unload_v1", 1), ("unload_v2", 1)]:
                    need(events.count(name) == count, "Unexpected event count " + name)
                need("constructor_timeout" not in events, "Constructor did not progress within its unchanged 2s fixture bound")
                need(set(events) <= {"load_accepted", "constructor_enter", "ack_attempt", "ack_written", "constructor_ack_seen", "upgrade_enter", "unload_v1", "unload_v2"}, "Unexpected original event; complete raw log retained")
                indices = [events.index(name) for name in ["constructor_enter", "ack_attempt", "constructor_ack_seen", "upgrade_enter"]]
                need(indices == sorted(indices) and len(set(indices)) == 4, "Required constructor/ACK/upgrade partial order differs")
                second_load = [n for n, event in enumerate(events) if event == "load_accepted"][1]
                need(indices[-1] < second_load < events.index("unload_v1") < events.index("unload_v2"), "Load/old-first cleanup order differs")
                need(events.index("ack_attempt") < events.index("ack_written"), "ACK write order differs")
                result.update(WholeNineFieldsVerified=True, WholeRawResult=snapshot(observed_path), WholeCleanup=snapshot(cleanup_path), Events=snapshot(case / "events.log"), EventValues=events, ConstructorBoundMilliseconds=2000, OriginalVmOuterSeconds=90)
            else:
                result["Errors"].append(dict(Stage="capture-files", Error="Whole result/cleanup missing; stderr/crashdump/owned process receipts retained"))
    except Exception as error:
        result["Errors"].append(dict(Stage="preparation-or-classification", Error=repr(error)))
    finally:
        try:
            guard(args)
            for row in sealed:
                need(sha(row["Path"]) == row["Sha256"], "Build input changed " + row["Path"])
            if provider_after is not None:
                after = provider_after()
                save(output / "provider-after.json", after)
                need(after == before["Inventory"], "Original provider/source changed")
        except Exception as error:
            result["Errors"].append(dict(Stage="final-source-provider-check", Error=repr(error)))
        result["Passed"] = not result["Errors"] and result.get("WholeNineFieldsVerified", False)
        result["NegativeControlsExecuted"] = False
        result["FullCompatibilityClaimed"] = False
        save(output / "completion.json", result)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ["repository", "otp-source", "build-receipt", "output"]:
        parser.add_argument("--" + key, type=Path, required=key == "repository")
    for key in ["expected-head", "plan-sha256", "admission-sha256"]:
        parser.add_argument("--" + key, required=key != "admission-sha256")
    parser.add_argument("--execute", action="store_true")
    parser.add_argument("--source-report", type=Path)
    args = parser.parse_args()
    repo, tool, plan = guard(args)
    result = execute(args, repo, tool, plan) if args.execute else dict(Passed=True, SourceOnly=True, NativeExecuted=False, OriginalExecuted=False, ManagedExecuted=False, Inputs=len(plan["Inputs"]))
    if args.source_report:
        args.source_report.parent.mkdir(parents=True, exist_ok=True)
        save(args.source_report, result)
    print(json.dumps(result, indent=2))
    return 0 if result["Passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
