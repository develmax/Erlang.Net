"""Original-only passive BEAM fun probe. Source author did not execute this file."""
import argparse, hashlib, importlib.util, json, os, shutil, sys
from pathlib import Path
sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def write(path, data):
    with Path(path).open("x", encoding="utf-8") as f:
        json.dump(data, f, indent=2)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def erlstr(path):
    return '"' + str(path).replace("\\", "/").replace('"', '\\"') + '"'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--official-admission", type=Path, required=True)
    parser.add_argument("--admission-sha256", required=True)
    args = parser.parse_args()
    if os.name != "nt":
        raise RuntimeError("This bounded proposal uses the existing admitted Windows provider/owned process helper")
    repo = args.repository.resolve()
    source = Path(__file__).resolve().parent
    seal = json.loads((source / "manifest.json").read_bytes())
    for row in seal["Inputs"]:
        if sha(repo / row["Path"]) != row["Sha256"]:
            raise RuntimeError("Original provider/helper/fixture changed: " + row["Path"])
    if sha(source / "beam_passive_fun647.erl") != seal["ObserverSha256"] or sha(source / "beam_passive_entry649.erl") != seal["EntrySha256"]:
        raise RuntimeError("Exact shared Source observer differs")
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    provider = load("fun647_provider", repo / "tools/Runtime/Erlang.OwnedGuest506Observations/collect.py")
    owned = load("fun647_owned", repo / "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py")
    args.build_receipt, args.otp_source, args.expected_head = None, None, None
    installed = inventory = after = None
    errors, modes, compiler_ok = [], [], False
    try:
        installed, erl, inventory, after = provider.provider(args, repo)
        write(output / "provider-before.json", inventory)
        work = output / "compiler"
        work.mkdir()
        shutil.copyfile(source / "beam_passive_fun647.erl", work / "beam_passive_fun647.erl")
        shutil.copyfile(source / "beam_passive_entry649.erl", work / "beam_passive_entry649.erl")
        fixture = repo / "tests/Erlang.Tests/Fixtures/CodePurgerOriginal432"
        for name in ["subject-v1.beam", "subject-v2.beam"]:
            shutil.copyfile(fixture / name, work / name)
        environment = os.environ.copy()
        for key in ["ERL_FLAGS", "ERL_AFLAGS", "ERL_ZFLAGS", "ROOTDIR", "BINDIR", "ERL_LIBS"]:
            environment.pop(key, None)
        environment["ERL_CRASH_DUMP"] = str(work / "erl_crash.dump")
        erlc = installed / "bin/erlc.exe"
        write(work / "executables.json", {"Runtime": {"Path": str(erl), "Sha256": sha(erl)}, "Compiler": {"Path": str(erlc), "Sha256": sha(erlc)}})
        owned.owned([str(erlc), "+debug_info", "-o", str(work), str(work / "beam_passive_fun647.erl"), str(work / "beam_passive_entry649.erl")], work, "compile", 60, environment, str(work))
        compiler_ok = True
        worker = work / "beam_passive_fun647.beam"
        for mode in ["complete", "abort", "reload"]:
            folder = output / mode
            folder.mkdir()
            passed = False
            try:
                expr = (
                    'W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
                    'Expected=filename:absname(' + erlstr(installed) + '),Expected=filename:absname(code:root_dir()),'
                    '8=erlang:system_info(wordsize),1=erlang:system_info(schedulers_online),'
                    'W("profile.etf",{erlang:system_info(wordsize),erlang:system_info(schedulers),erlang:system_info(schedulers_online),erlang:system_info(debug_compiled),erlang:system_info(otp_release)}),'
                    '{ok,A}=file:read_file(' + erlstr(work / 'subject-v1.beam') + '),'
                    '{ok,B}=file:read_file(' + erlstr(work / 'subject-v2.beam') + '),'
                    'beam_passive_fun647:module_info(),P=filename:absname(' + erlstr(worker) + '),P=filename:absname(code:which(beam_passive_fun647)),'
                    '{ok,{beam_passive_fun647,D}}=beam_lib:md5(P),D=beam_passive_fun647:module_info(md5),'
                    'W("worker-owner.etf",{P,D,beam_passive_fun647:module_info(compile)}),'
                    'W("purger-owner.etf",{code:which(erts_code_purger),erts_code_purger:module_info(md5)}),'
                    'beam_passive_entry649:module_info(),Q=filename:absname(' + erlstr(work / 'beam_passive_entry649.beam') + '),Q=filename:absname(code:which(beam_passive_entry649)),'
                    '{ok,{beam_passive_entry649,E}}=beam_lib:md5(Q),E=beam_passive_entry649:module_info(md5),W("entry-owner.etf",{Q,E,beam_passive_entry649:module_info(compile)}),'
                    'Result=beam_passive_entry649:run(' + mode + ',A,B),{whole_outcome,_,unchanged_full_stack,S}=Result,W("stack.etf",S),W("observed.etf",Result),io:format("~tp~n",[Result]),halt(0).'
                )
                (folder / "invocation.eval").write_text(expr, encoding="utf-8")
                owned.owned([str(erl), "+S", "1:1", "+A", "1", "-noshell", "-noinput", "-boot", "no_dot_erlang", "-pa", str(work), "-eval", expr], folder, "original", 60, dict(environment, ERL_CRASH_DUMP=str(folder / "erl_crash.dump")), str(folder))
                if not (folder / "observed.etf").is_file():
                    raise RuntimeError("Original whole outcome absent")
                passed = True
            except Exception as error:
                errors.append({"Mode": mode, "Error": repr(error)})
            modes.append({"Mode": mode, "Executed": (folder / "original.live.json").is_file(), "PassedExecution": passed})
    except Exception as error:
        errors.append({"Stage": "prepare/compiler", "Error": repr(error)})
    finally:
        if after is not None:
            try:
                current = after()
                equal = current == inventory["Inventory"]
                write(output / "provider-after.json", {"ProviderEqual": equal, "Inventory": current})
                if not equal:
                    errors.append({"Stage": "provider-after", "Error": "Provider changed"})
            except Exception as error:
                errors.append({"Stage": "provider-after", "Error": repr(error)})
        write(output / "completion.json", {"SourcePin": PIN, "OriginalOnly": True, "CompilerPassed": compiler_ok, "Modes": modes, "Errors": errors, "SemanticAssertions": False, "Normalized": False, "Files": [{"Path": str(p.relative_to(output)), "Bytes": p.stat().st_size, "Sha256": sha(p)} for p in output.rglob("*") if p.is_file()]})
    return 0 if not errors and len(modes) == 3 and all(x["PassedExecution"] for x in modes) else 1


if __name__ == "__main__":
    sys.exit(main())
