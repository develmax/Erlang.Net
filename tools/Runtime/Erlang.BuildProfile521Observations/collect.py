"""One fresh original build-profile521 worker; delegates admission/process ownership."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys

sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
TARGET = "tools/Runtime/Erlang.BuildProfile521Observations"


def base(repository):
    path = repository / "tools/Runtime/Erlang.OwnedGuest506Observations/collect.py"
    spec = importlib.util.spec_from_file_location("build_profile521_existing_observer", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def expression():
    return ('W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
        'try Expected=filename:absname(os:getenv("CONTRACT506_ORIGINAL_ROOT")),'
        'Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
        'W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(otp_release),'
        'erlang:system_info(build_type),erlang:system_info(debug_compiled),'
        'erlang:system_info(emu_flavor),erlang:system_info(system_architecture),erlang:system_info(schedulers)}),'
        'Files=["build_profile521.erl","contract_worker513.erl"],'
        'Compiled=[compile:file(F,[return_errors,return_warnings])||F<-Files],'
        'W("compile.etf",Compiled),true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,Compiled),'
        'Modules=[build_profile521,contract_worker513],'
        'W("executing-owners.etf",[{M,code:which(M),M:module_info(md5),M:module_info(compile),'
        'M:module_info(attributes),M:module_info(exports)}||M<-Modules]),'
        'T=contract_worker513:run(build_profile521),W("observed.etf",T),'
        'ok=file:write_file("observed-deterministic.etf",term_to_binary(T,[deterministic,{minor_version,2}])),'
        'W("worker-witnesses.etf",get(contract_worker513_witnesses)),'
        '{{returned,{Rows,{1,true},Invalid}},normal}=T,9=length(Rows),4=length(Invalid),'
        '{build_type,{returned,Build}}=lists:keyfind(build_type,1,Rows),'
        '{emu_type,{returned,Build}}=lists:keyfind(emu_type,1,Rows),'
        '{debug_compiled,{returned,Debug}}=lists:keyfind(debug_compiled,1,Rows),'
        '{smp_support,{returned,true}}=lists:keyfind(smp_support,1,Rows),'
        'ProfileValid=(is_atom(Build) andalso (Debug =:= (Build =:= debug))),'
        'W("profile-predicate.etf",{Build,Debug,ProfileValid}),true=ProfileValid,'
        'true=lists:all(fun({_,{raised,error,badarg,Stack}}) when is_list(Stack),Stack=/=[]->true;(_)->false end,Invalid),'
        'Cleanup=[{M,code:delete(M),code:purge(M),code:is_loaded(M)}||M<-Modules],'
        'W("cleanup.etf",Cleanup),true=lists:all(fun({_,_,_,false})->true;(_)->false end,Cleanup),halt(0)'
        ' catch Class:Reason:Stack->W("entry-failure.etf",{Class,Reason,Stack}),halt(77) end.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--plan-sha256", required=True)
    parser.add_argument("--official-admission", type=Path, required=True)
    parser.add_argument("--admission-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--execute-original", action="store_true")
    args = parser.parse_args()
    repo, tool, out = args.repository.resolve(), Path(__file__).resolve().parent, args.output.resolve()
    helper = base(repo)
    helper.require(tool == repo / TARGET, "Promote collector to its declared target first")
    plan_path = tool / "source-plan.json"
    helper.require(helper.sha(plan_path) == args.plan_sha256.upper(), "Source plan differs")
    plan = json.loads(plan_path.read_bytes())
    helper.require(plan["PinnedCommit"] == PIN, "Source pin differs")
    helper.check_inputs(repo, tool, plan)
    helper.require(out.is_relative_to(repo / "artifacts") and not out.exists(), "Fresh contained output required")
    out.mkdir(parents=True)
    helper.save(out / "launch-plan.json", {"ExecuteOriginal": args.execute_original,
        "PlanSha256": helper.sha(plan_path), "ManagedExecuted": False, "WholeContractsClosed": 0})
    if not args.execute_original:
        return 0
    helper.require(os.name == "nt", "This first acquisition uses official Windows provider; Linux needs its own admission")
    args.build_receipt = args.otp_source = args.expected_head = None
    try:
        installed, executable, before, provider_after = helper.provider(args, repo)
    except Exception as error:
        helper.save(out / "preflight-failure.json", {"Error": repr(error), "VmStarted": False})
        helper.save(out / "completion.json", {"ObservationAcquisitionPassed": False,
            "VmStarted": False, "ManagedExecuted": False, "Errors": [repr(error)]})
        return 1
    helper.save(out / "provider-before.json", before)
    errors, receipt = [], None
    directory = out / "build-profile521"
    directory.mkdir()
    try:
        for name in ("build_profile521.erl", "contract_worker513.erl"):
            shutil.copyfile(tool / "fixtures" / name, directory / name)
        env = dict(os.environ)
        for key in ("ERL_FLAGS", "ERL_AFLAGS", "ERL_ZFLAGS", "ERL_LIBS", "ERL_COMPILER_OPTIONS", "ERLC_EMULATOR",
                    "ERLC_USE_SERVER", "ERLC_SERVER_ID", "ERLC_CONFIGURATION", "ERL_ROOTDIR"):
            env.pop(key, None)
        env["CONTRACT506_ORIGINAL_ROOT"] = str(installed)
        env["ERL_CRASH_DUMP"] = str(directory / "erl_crash.dump")
        env["ERL_CRASH_DUMP_SECONDS"] = "15"
        command = [str(executable), "+S", "1:1", "+A", "1", "-noshell", "-noinput", "-boot", "no_dot_erlang",
            "-pa", str(directory), "-eval", expression()]
        helper.save(directory / "launch.json", {"Command": command})
        receipt = helper.execute(repo, command, directory, env)
    except Exception as error:
        errors.append(repr(error))
    finally:
        try:
            after = provider_after()
            helper.save(out / "provider-after.json", after)
            helper.require(after == before["Inventory"], "Provider closure changed")
            helper.require(helper.sha(before["AdmissionPath"]) == before["AdmissionSha256"], "Provider admission changed")
            for row in before["RelatedAdmissions"]:
                helper.require(helper.sha(row["Path"]) == row["Sha256"], "Related provider admission changed")
            helper.check_inputs(repo, tool, plan)
            helper.require(helper.sha(plan_path) == args.plan_sha256.upper(), "Source plan changed")
        except Exception as error:
            errors.append("Final source/provider guard: " + repr(error))
        rows = [{"Path": p.relative_to(out).as_posix(), "Sha256": helper.sha(p), "Bytes": p.stat().st_size}
            for p in sorted(out.rglob("*")) if p.is_file() and p.name not in ("artifacts.json", "completion.json")]
        helper.save(out / "artifacts.json", rows)
        helper.save(out / "completion.json", {"ObservationAcquisitionPassed": not errors,
            "SourcePin": PIN, "SourcePlanSha256": helper.sha(plan_path), "ProviderFiles": len(before["Inventory"]),
            "OriginalProcessReceipt": receipt, "ArtifactInventorySha256": helper.sha(out / "artifacts.json"),
            "ManagedExecuted": False, "RawOutputsRewritten": False, "WholeContractsClosed": 0, "Errors": errors})
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
