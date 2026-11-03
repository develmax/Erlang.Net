"""One real original variadic family, optional existing four-route managed replay."""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
import importlib.util
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[3]
TOOL = Path(__file__).resolve().parent
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--official-admission", type=Path, required=True)
    parser.add_argument("--admission-sha256", required=True)
    parser.add_argument("--plan-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--execute-original", action="store_true")
    parser.add_argument("--with-managed", action="store_true")
    parser.add_argument("--runtime-version", default="10.0.11")
    args = parser.parse_args()
    helper = load(ROOT / "tools/Runtime/Erlang.OwnedGuest506Observations/collect.py", "existing_variadic526")
    sha, save, require = helper.sha, helper.save, helper.require
    require(not args.with_managed or args.execute_original, "Managed replay requires actual original acquisition")
    plan_path = TOOL / "source-plan.json"
    require(sha(plan_path) == args.plan_sha256.upper(), "Source plan differs")
    plan = json.loads(plan_path.read_bytes())
    require(plan["PinnedCommit"] == PIN and os.name == "nt", "Windows pinned provider only")
    helper.check_inputs(ROOT, TOOL, plan)
    out = args.output.resolve()
    require(out.is_relative_to(ROOT / "artifacts/nif-variadic526") and not out.exists(), "Fresh contained output required")
    out.mkdir(parents=True)
    save(out / "launch-plan.json", {"Execute": args.execute_original, "Managed": args.with_managed, "PlanSha256": sha(plan_path)})
    if not args.execute_original:
        return 0
    args.build_receipt = args.otp_source = args.expected_head = None
    installed, erl, before, after_provider = helper.provider(args, ROOT)
    save(out / "provider-before.json", before)
    phases, errors = {}, []
    env = dict(os.environ, MAP_FAMILY504_RUNTIME_ROOT=str(installed), ERL_CRASH_DUMP="NUL")
    for key in ("ERL_FLAGS", "ERL_AFLAGS", "ERL_ZFLAGS", "ERL_LIBS", "ERL_COMPILER_OPTIONS", "ERLC_EMULATOR"):
        env.pop(key, None)
    def phase(name, command, directory):
        try:
            directory.mkdir()
            phases[name] = helper.execute(ROOT, command, directory, env)
            return True
        except Exception as error:
            errors.append(name + ": " + repr(error))
            phases[name] = {"Failed": True, "Error": repr(error)}
            return False
    matched = []
    try:
        build_ok = phase("build", [sys.executable, "-B", str(TOOL / "build-native.py"), "--plan", str(plan_path),
            "--approved-plan-sha256", args.plan_sha256, "--provider-admission", str(args.official_admission.resolve()),
            "--output", str(out / "build")], out / "prepare")
        if build_ok:
            build_path = out / "build/build-receipt.json"
            build = json.loads(build_path.read_bytes())
            require(build["Status"] == "VARIADIC526_WINDOWS_BUILD_COMPLETE", "Wrong native receipt")
            library = out / "build/variadic_family526.dll"
            require(sha(library) == build["LibrarySha256"], "Native image differs")
            original = out / "original"
            original.mkdir()
            for name in ("variadic_family526.erl", "variadic_family526_driver.erl"):
                shutil.copyfile(TOOL / "fixtures" / name, original / name)
            stem = library.with_suffix("").as_posix()
            expression = ('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
                'try Expected=filename:absname(os:getenv("MAP_FAMILY504_RUNTIME_ROOT")),'
                'Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
                'W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(system_architecture)}),'
                'Files=["variadic_family526.erl","variadic_family526_driver.erl"],'
                'C=[compile:file(F,[return_errors,return_warnings])||F<-Files],W("compile.etf",C),'
                'true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,C),'
                'W("executing-owners.etf",[{M,code:which(M),M:module_info(md5),M:module_info(compile),M:module_info(attributes),M:module_info(exports)}||M<-[variadic_family526,variadic_family526_driver]]),'
                'T=variadic_family526_driver:observe(' + json.dumps(stem) + ',probe),W("observed.etf",T),'
                'ok=file:write_file("observed-deterministic.etf",term_to_binary(T,[deterministic,{minor_version,2}])),'
                '{loaded,[{1,_,_,_,_,_,_,_,1},{2,_,_,_,_,_,_,_,1},{3,_,_,_,_,_,_,_,1}]}=T,'
                'D=code:delete(variadic_family526),P=code:purge(variadic_family526),L=code:is_loaded(variadic_family526),W("cleanup.etf",{D,P,L}),false=L,halt(0)'
                ' catch Class:Reason:Stack->W("entry-failure.etf",{Class,Reason,Stack}),halt(77) end.')
            # Directory is prepared already; execute helper owns the real VM.
            command = [str(erl), "+S", "1:1", "+A", "1", "+SDcpu", "1", "+SDio", "1", "-noshell", "-noinput", "-boot", "no_dot_erlang", "-pa", str(original), "-eval", expression]
            save(original / "launch.json", {"Command": command})
            original_ok = False
            try:
                phases["original"] = helper.execute(ROOT, command, original, env)
                original_ok = True
            except Exception as error:
                errors.append("original: " + repr(error))
            if original_ok:
                lists = list(installed.glob("lib/stdlib-*/ebin/lists.beam"))
                require(len(lists) == 1, "Unique original lists image required")
                image_paths = [lists[0], original / "variadic_family526.beam", original / "variadic_family526_driver.beam"]
                admission = {"Status": "SOURCE_BOUND_ORIGINAL_DIRTY521_JOB", "Pin": PIN, "Mode": "probe",
                    "SubjectKind": "GENUINE_VARIADIC_FAMILY526", "ObserverModule": "variadic_family526_driver", "NifModule": "variadic_family526",
                    "NativeLibraryPath": str(library), "NativeLibrarySha256": sha(library), "BuildReceiptPath": str(build_path), "BuildReceiptSha256": sha(build_path),
                    "Images": [{"Module": p.stem, "Path": str(p), "Sha256": sha(p)} for p in image_paths],
                    "Dependencies": ["erlang", "lists", "variadic_family526", "variadic_family526_driver"],
                    "ControlDependencies": [], "OriginalRawPath": str(original / "observed.etf"), "OriginalDeterministicPath": str(original / "observed-deterministic.etf"),
                    "OriginalCleanupPath": str(original / "cleanup.etf"), "Inputs": [{"Path": str(p), "Sha256": sha(p)} for p in [plan_path, build_path, original / "compile.etf", original / "executing-owners.etf", original / "metadata.etf", original / "observed.etf", original / "observed-deterministic.etf", original / "cleanup.etf"]]}
                admission_path = out / "execution-admission.json"
                save(admission_path, admission)
                if args.with_managed:
                    host = ROOT / "tools/Native/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll"
                    require(host.is_file(), "Managed host build required; originals retained")
                    managed_inputs = [{"Path": str(path), "Sha256": sha(path)}
                        for path in sorted(host.parent.rglob("*")) if path.is_file() and path.suffix in (".dll", ".json")]
                    save(out / "managed-inputs-before.json", managed_inputs)
                    for profile in ("ordinary", "private-owned"):
                        for route in ("ordinary", "reviewed"):
                            name = profile + "-" + route
                            actual = out / name
                            success = phase(name, ["dotnet", "exec", "--fx-version", args.runtime_version, str(host), "--nif-dirty521-child", str(admission_path), sha(admission_path), route, profile, str(actual / "actual")], actual)
                            if success:
                                try:
                                    receipt = json.loads((actual / "actual/completion.json").read_bytes())
                                    require(receipt["ChildPassedFiniteRawComparison"] and receipt["RawComparisonPassed"]
                                        and receipt["ExecutionAndCleanupPassed"] and receipt["NativeEnvironmentAndBinaryBaselinesRestored"]
                                        and receipt["NativeModuleAbsentBeforeAndAfter"], "Managed full comparison/cleanup failed")
                                    require(receipt["OwnedProfile"] == (profile == "private-owned") and receipt["Route"] == route
                                        and receipt["AdmissionSha256"] == sha(admission_path), "Managed profile/route/admission differs")
                                    matched.append(name)
                                except Exception as error:
                                    errors.append(name + ": receipt: " + repr(error))
                    managed_after = [{"Path": str(path), "Sha256": sha(path)}
                        for path in sorted(host.parent.rglob("*")) if path.is_file() and path.suffix in (".dll", ".json")]
                    save(out / "managed-inputs-after.json", managed_after)
                    require(managed_inputs == managed_after, "Actual managed/asset input bytes changed")
    except Exception as error:
        errors.append("batch: " + repr(error))
    finally:
        try:
            after = after_provider()
            save(out / "provider-after.json", after)
            require(after == before["Inventory"], "Provider changed")
            helper.check_inputs(ROOT, TOOL, plan)
        except Exception as error:
            errors.append("final guards: " + repr(error))
        rows = [{"Path": p.relative_to(out).as_posix(), "Sha256": sha(p), "Bytes": p.stat().st_size} for p in sorted(out.rglob("*")) if p.is_file() and p.name not in ("artifacts.json", "completion.json")]
        save(out / "artifacts.json", rows)
        save(out / "completion.json", {"Passed": not errors, "ProviderFiles": len(before["Inventory"]), "PlanSha256": sha(plan_path), "InventorySha256": sha(out / "artifacts.json"), "OriginalExecuted": "original" in phases, "ManagedRequested": args.with_managed, "MatchedManagedRoutes": matched, "ExpectedManagedRoutes": 4 if args.with_managed else 0, "Phases": phases, "Errors": errors, "NormalizationPerformed": False, "WholeContractsClosed": 0})
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
