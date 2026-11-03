"""Acquire complete dirty-NIF521 originals; default prepares only, never projects ETF."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys

sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
ROOT = Path(__file__).resolve().parents[3]
TOOL = Path(__file__).resolve().parent


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def source_proof(plan):
    result = []
    for row in plan["Dependencies"]:
        path = (ROOT / row["Path"]).resolve()
        if not path.is_relative_to(ROOT) or sha(path) != row["Sha256"]:
            raise RuntimeError("Source dependency changed: " + row["Path"])
        result.append(dict(row))
    return result


def erl_string(path):
    text = str(path).replace("\\", "/")
    if any(ord(c) < 32 or ord(c) > 126 for c in text):
        raise ValueError("Fresh native lifecycle paths must be printable ASCII")
    return json.dumps(text)


def expression(job, library, installed, directory):
    write = 'W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
    metadata = ('Expected=filename:absname(os:getenv("DIRTY521_RUNTIME_ROOT")),'
        'Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
        '1=erlang:system_info(dirty_cpu_schedulers_online),'
        '2=erlang:system_info(dirty_io_schedulers),'
        'W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(system_architecture),'
        'erlang:system_info(schedulers),erlang:system_info(dirty_cpu_schedulers_online),'
        'erlang:system_info(dirty_io_schedulers)}),')
    compile_sources = ('Files=["dirty_flags521.erl","dirty_flags521_driver.erl"],'
        'Compiled=[compile:file(F,[return_errors,return_warnings])||F<-Files],'
        'W("compile.etf",Compiled),'
        'true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,Compiled),'
        'W("executing-owners.etf",[{M,code:which(M),M:module_info(md5),M:module_info(compile),'
        'M:module_info(attributes),M:module_info(exports)}||M<-[dirty_flags521,dirty_flags521_driver]]),')
    lib = erl_string(library)
    if job["Mode"] == "control":
        barrier, marker = erl_string(directory / "release521"), erl_string(directory / "unloaded521")
        call = 'dirty_flags521_driver:observe_control(' + lib + ',' + barrier + ',' + marker + ',{' + job["Kind"] + ',' + job["Lifecycle"] + '})'
        acceptance = 'case Observed of {control521,' + job["Kind"] + ',' + job["Lifecycle"] + ',_,_,_,_,_,_,_,_,_,_,_,_,_,_}->true;_->false end'
    else:
        call = 'dirty_flags521_driver:observe(' + lib + ',' + job["Mode"] + ')'
        if job["ExpectedLoad"] == "bad_lib":
            acceptance = 'case Observed of {not_loaded,{error,{bad_lib,Text}}} when is_list(Text),Text=/=[]->true;_->false end'
        else:
            acceptance = 'case Observed of {loaded,_}->true;_->false end'
    finish = ('Observed=' + call + ',W("observed.etf",Observed),'
        'ok=file:write_file("observed-deterministic.etf",term_to_binary(Observed,[deterministic,{minor_version,2}])),'
        'Accepted=(' + acceptance + '),W("protocol-accepted.etf",Accepted),'
        'D=code:delete(dirty_flags521),P=code:purge(dirty_flags521),L=code:is_loaded(dirty_flags521),'
        'W("cleanup.etf",{D,P,L}),false=L,'
        'case Accepted of true->halt(0);false->halt(78) end')
    return write + 'try ' + metadata + compile_sources + finish + ' catch Class:Reason:Stack->W("entry-failure.etf",{Class,Reason,Stack}),halt(77) end.'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--provider-admission", type=Path, required=True)
    parser.add_argument("--provider-sha256", required=True)
    parser.add_argument("--plan-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--jobs", help="Comma-separated existing names; fresh output directory required")
    parser.add_argument("--execute-original521", action="store_true")
    args = parser.parse_args()
    plan_path = TOOL / "source-plan.json"
    if sha(plan_path) != args.plan_sha256.upper():
        raise RuntimeError("Unapproved source-plan bytes")
    plan = json.loads(plan_path.read_bytes())
    if plan["PinnedCommit"] != PIN or os.name != "nt" or sys.maxsize <= 2**32:
        raise RuntimeError("This packet requires admitted Windows x64 OTP29.1.1")
    proof = source_proof(plan)
    admission_path = args.provider_admission.resolve()
    if sha(admission_path) != args.provider_sha256.upper():
        raise RuntimeError("Provider admission bytes changed")
    admission = json.loads(admission_path.read_bytes())
    if admission["SourceCommit"] != PIN or admission["ReleaseTag"] != "OTP-29.1.1":
        raise RuntimeError("Wrong original provider")
    helpers = ROOT / "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts"
    owned = load("owned_dirty521", helpers / "owned.py")
    inventory = load("inventory_dirty521", helpers / "bounded_inventory.py")
    installed = Path(admission["InstalledOtpRoot"]).resolve()
    provider_manifest = Path(admission["ExecutionManifestPath"])
    if sha(provider_manifest) != admission["ExecutionManifestSha256"]:
        raise RuntimeError("Provider manifest changed")
    expected = {row["Path"].replace("\\", "/"): row["Sha256"] for row in json.loads(provider_manifest.read_bytes())}
    before = inventory.inventory(installed)
    if len(expected) != 6427 or before != expected:
        raise RuntimeError("Provider closure changed")
    selection = set(args.jobs.split(",")) if args.jobs else set(plan["Jobs"])
    if not selection or selection - set(plan["Jobs"]):
        raise RuntimeError("Unknown/empty job selection")
    out = args.output.resolve()
    if not out.is_relative_to(ROOT / "artifacts/nif-dirty521") or out.exists():
        raise RuntimeError("Fresh contained output directory required")
    erl_string(out)
    out.mkdir(parents=True)
    save(out / "provider-before.json", before)
    save(out / "dependency-proof.json", proof)
    save(out / "selection.json", {"Jobs": sorted(selection), "PlanSha256": sha(plan_path), "Execute": args.execute_original521})
    if not args.execute_original521:
        save(out / "completion.json", {"PreparedOnly": True, "OriginalExecuted": False, "ManagedExecuted": False})
        return 0
    env = dict(os.environ)
    env["DIRTY521_RUNTIME_ROOT"] = str(installed)
    env["ERL_CRASH_DUMP"] = "NUL"
    for key in ("ERL_FLAGS", "ERL_AFLAGS", "ERL_ZFLAGS", "ERL_LIBS", "ERL_COMPILER_OPTIONS", "ERLC_EMULATOR"):
        env.pop(key, None)
    phases, builds, errors = {}, {}, []

    def phase(name, command, directory, cwd):
        try:
            directory.mkdir(parents=True, exist_ok=False)
            receipt = owned.owned(command, directory, name, 180, env, str(cwd))
            phases[name] = receipt
            return True
        except Exception as error:
            phases[name] = {"CollectionFailed": True, "Error": repr(error)}
            receipt_path = directory / (name + ".exit.json")
            try:
                if receipt_path.is_file():
                    phases[name] = json.loads(receipt_path.read_bytes())
            except Exception as receipt_error:
                errors.append(name + " receipt: " + repr(receipt_error))
            errors.append(name + ": " + repr(error))
            return False

    try:
        variants = sorted({plan["Jobs"][name]["Variant"] for name in selection})
        for variant in variants:
            build = out / ("build-" + variant)
            ok = phase("build-" + variant, [sys.executable, "-B", str(TOOL / "build-native.py"),
                "--plan", str(plan_path), "--approved-plan-sha256", args.plan_sha256,
                "--provider-admission", str(admission_path), "--variant", variant, "--output", str(build)],
                out / ("prepare-" + variant), ROOT)
            if ok:
                try:
                    receipt = json.loads((build / "build-receipt.json").read_bytes())
                    if receipt["Status"] != "DIRTY521_WINDOWS_BUILD_COMPLETE" or receipt["Variant"] != variant:
                        raise RuntimeError("Wrong variant build receipt")
                    if sha(build / "dirty_flags521.dll") != receipt["LibrarySha256"]:
                        raise RuntimeError("Built image changed")
                except Exception as error:
                    errors.append("build proof " + variant + ": " + repr(error))
                    ok = False
            builds[variant] = ok
        for name, job in plan["Jobs"].items():
            if name not in selection:
                continue
            if not builds.get(job["Variant"]):
                phases[name] = {"SkippedDependentPreparationFailure": True, "Variant": job["Variant"]}
                continue
            directory = out / name
            try:
                directory.mkdir()
                for leaf in ("dirty_flags521.erl", "dirty_flags521_driver.erl"):
                    shutil.copyfile(TOOL / "fixtures" / leaf, directory / leaf)
                command = [str(installed / "bin/erl.exe"), "+S", "1:1", "+A", "1", "+SDcpu", "1", "+SDio", "2",
                    "-noshell", "-noinput", "-boot", "no_dot_erlang", "-pa", str(directory), "-eval",
                    expression(job, out / ("build-" + job["Variant"]) / "dirty_flags521", installed, directory)]
                # Retain exact launch text before starting the owned process.
                save(directory / "launch.json", {"Command": command, "Variant": job["Variant"], "Job": name})
                phase(name, command, out / (name + "-process"), directory)
                if job["Mode"] == "control":
                    marker_rows = []
                    for leaf in ("release521", "unloaded521", "release521.entered", "release521.done",
                                 "release521.tmp", "unloaded521.tmp", "release521.entered.tmp", "release521.done.tmp"):
                        marker = directory / leaf
                        row = {"Path": leaf, "Exists": marker.is_file()}
                        if marker.is_file():
                            raw = marker.read_bytes()
                            row.update(Sha256=sha(marker), Bytes=len(raw), ExactBytesHex=raw.hex().upper())
                        marker_rows.append(row)
                    save(directory / "markers-state.json", marker_rows)
            except Exception as error:
                errors.append(name + " preparation: " + repr(error))
    finally:
        try:
            after = inventory.inventory(installed)
        except Exception as error:
            after = {}
            errors.append("Provider after-inventory failure: " + repr(error))
        save(out / "provider-after.json", after)
        if after != expected:
            errors.append("Original provider closure changed after acquisition")
        try:
            source_proof(plan)
        except Exception as error:
            errors.append("Final source guard: " + repr(error))
        rows = []
        for path in sorted(out.rglob("*")):
            if path.is_file() and path.name not in ("completion.json", "artifacts.json"):
                rows.append({"Path": path.relative_to(out).as_posix(), "Sha256": sha(path), "Bytes": path.stat().st_size})
        save(out / "artifacts.json", rows)
        save(out / "completion.json", {"ObservationAcquisitionPassed": not errors,
            "SourcePin": PIN, "Platform": "windows-x64", "PlanSha256": sha(plan_path),
            "AdmissionSha256": sha(admission_path), "ProviderFiles": len(expected),
            "ProviderBeforeEqual": before == expected, "ProviderAfterEqual": after == expected,
            "ArtifactInventorySha256": sha(out / "artifacts.json"),
            "ExpectedSelectedJobs": len(selection), "NativeBuildsAttempted": len(builds),
            "OriginalJobsAttempted": sum(name in phases and not phases[name].get("SkippedDependentPreparationFailure", False) for name in selection),
            "ManagedExecuted": False, "NormalizationPerformed": False, "WholeContractsClosed": 0,
            "Phases": phases, "Errors": errors})
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
