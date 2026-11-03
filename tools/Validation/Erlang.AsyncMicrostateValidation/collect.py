"""Source-bound Windows actual async observer. Reuses catalog package, builds no native4C bridge."""
import argparse,hashlib,importlib.util,json,os,shutil,sys
from pathlib import Path
from types import SimpleNamespace
from windows_process import run
PIN="ad05823719d77c8faee87348ea39513d4e2f99c5"
LEGACY_CASE_ROSTER = ('original-driver-A1', 'original-driver-A2', 'public-queued-ack-cancel', 'public-queued-ack-close', 'component-child-exit', 'component-wrong-epoch', 'original-queued-port-close-A1', 'component-child-reservation-rollback')
PENDING_CASE_ROSTER = ('public-pending-rpc-capture-kill', 'public-pending-rpc-capture-close', 'public-pending-rpc-apply-kill', 'public-pending-rpc-apply-close', 'public-pending-rpc-finish-kill', 'public-pending-rpc-finish-close', 'public-pending-rpc-release-kill', 'public-pending-rpc-release-close', 'component-pending-rpc-gate-unarmed', 'component-pending-rpc-gate-wrong-operation', 'component-pending-rpc-gate-dispose-before', 'component-pending-rpc-gate-duplicate-release', 'component-pending-rpc-gate-child-death', 'public-pending-rpc-noop-release-kill', 'public-pending-rpc-noop-release-close')
EXPECTED_CASE_ROSTER = LEGACY_CASE_ROSTER + PENDING_CASE_ROSTER
def need(value,why):
    if not value:raise ValueError(why)
def sha(path,mode="Raw"):
    raw=Path(path).read_bytes()
    return hashlib.sha256(raw.replace(b"\r\n",b"\n")if mode=="TextLF"else raw).hexdigest().upper()
def save(path,value):Path(path).write_text(json.dumps(value,indent=2)+"\n",encoding="utf-8")
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
def sealed(plan,repo):
    for row in plan["Inputs"]:
        original=repo/row["Path"];path=original.resolve();need(path.is_relative_to(repo)and path.is_file(),"Missing/escaping source input: "+row["Path"])
        for member in [original,*original.parents]:
            if member == repo.parent:break
            need(not member.is_symlink() and not getattr(member.lstat(),"st_file_attributes",0)&0x400,"Reparse source input")
        need(sha(path,row["HashMode"])==row["Sha256"],"Source input changed: "+row["Path"])
def package_inputs(path):
    root=path.parent;value=json.loads(path.read_bytes());need(value["Protocol"]==1 and value["EntryPoint"]=="Erlang.DriverHost.exe","Existing Windows catalog package required")
    result=[];seen=set()
    for row in value["Files"]:
        p=(root/row["Path"]).resolve();need(p.is_relative_to(root)and p.is_file()and str(p).lower()not in seen,"Bad package member");seen.add(str(p).lower());need(sha(p)==row["Sha256"],"Package member changed");result.append(dict(Path=str(p),Sha256=sha(p)))
    need(result,"Empty package");return result

def main():
    parser=argparse.ArgumentParser()
    for name in ["repository","admission","package","output"]:parser.add_argument("--"+name,type=Path,required=True)
    parser.add_argument("--configuration",choices=["Debug","Release"],required=True);parser.add_argument("--plan-sha256",required=True)
    args=parser.parse_args();repo=args.repository.resolve();tool=Path(__file__).resolve().parent;out=args.output.resolve();planpath=tool/"source-plan.json"
    need(os.name=="nt"and sys.maxsize>2**32,"Actual native DriverHost Linux is unsupported; this gate is Windowsx64 only")
    need(sys.dont_write_bytecode,"Use Python -B");need(sha(planpath)==args.plan_sha256.upper(),"Source-plan byte pin differs")
    plan=json.loads(planpath.read_bytes());need(plan["Version"]==1 and plan["OtpPin"]==PIN,"Wrong source plan");need(plan["Cases"]==list(EXPECTED_CASE_ROSTER),"Source-plan exact mandatory23 roster differs");sealed(plan,repo)
    need(not out.exists()and out.is_relative_to(repo/"artifacts"),"Fresh repository artifact output required");out.mkdir(parents=True)
    errors=[];stages=[];before=None;after=None;package=None;packagebefore=None;observer_inputs=None;execution_tools={};sourcebefore=[dict(Path=r["Path"],Sha256=sha(repo/r["Path"],r["HashMode"]),HashMode=r["HashMode"])for r in plan["Inputs"]]
    env={k:v for k,v in os.environ.items()if not any(secret in k.upper()for secret in ["TOKEN","PASSWORD","SECRET","CREDENTIAL"])}
    env.update(DOTNET_CLI_USE_MSBUILD_SERVER="0",MSBUILDDISABLENODEREUSE="1")
    for key in ["CL","_CL_","LINK","_LINK_"]:need(not env.get(key),"Unadmitted compiler option override: "+key)
    def bound_tool(name,path):
        path=Path(path).resolve(strict=True);need(path.is_file(),"Executable/setup input missing: "+name)
        execution_tools[name]=dict(Path=str(path),Sha256=sha(path),Bytes=path.stat().st_size)
        return path
    def skipped(name,dependencies):
        stages.append(dict(Name=name,Passed=False,SkippedDependency=True,Dependencies=dependencies))
        errors.append(dict(Stage=name,Error="Required prerequisite failed",Dependencies=dependencies))
    def stage(name,command,cwd=out,timeout=90,process_environment=None,compiler_retirement=None):
        try:
            need(Path(command[0]).is_absolute() and Path(command[0]).is_file(), "Owned lpApplicationName must be an actual absolute executable")
            need(compiler_retirement is None or name == "fixture-native", "Compiler ancillary profile is limited to fixture preparation")
            receipt=run([str(x)for x in command],out,name,timeout,env if process_environment is None else process_environment,cwd,compiler_retirement=compiler_retirement)
            common=receipt["ExitCode"]==0 and not receipt["TimedOut"] and receipt["ActiveProcessesAfter"]==0 and receipt["ResourcesClosed"] and not receipt["Errors"]
            retirement=receipt.get("CompilerRetirement",{})
            declared=False
            if compiler_retirement is not None and common:
                artifact=retirement.get("OutputBeforeRetirement",{})
                output=Path(compiler_retirement["OutputPath"])
                expected_artifact=dict(Path=str(output),Bytes=output.stat().st_size,Sha256=sha(output))
                base=(retirement.get("PolicyVersion")==1 and retirement.get("Kind")=="DeclaredMSVCCompilerAncillary" and retirement.get("Success") is True and retirement.get("InspectionHandleClosed") is True and artifact==expected_artifact)
                natural=(base and retirement.get("Disposition")=="Natural" and retirement.get("NaturalDescendantsExit") is True and retirement.get("TerminationRequested") is False and receipt["ForcedCleanup"] is False)
                retired=(base and retirement.get("Disposition")=="DeclaredAncillaryRetirement" and retirement.get("NaturalDescendantsExit") is False and retirement.get("TerminationRequested") is True and receipt["ForcedCleanup"] is True and retirement.get("MembershipVerified") is True and os.path.normcase(retirement.get("Image",""))==os.path.normcase(compiler_retirement["AncillaryPath"]) and retirement.get("ImageSha256")==compiler_retirement["AncillarySha256"] and retirement.get("RequestedExitCode")==0xE0011288 and retirement.get("ObservedExitCode")==0xE0011288 and retirement.get("JobActiveAfterRetirement")==0 and retirement.get("OutputAfterRetirement")==expected_artifact and type(retirement.get("Pid")) is int and retirement["Pid"]>0)
                declared=natural or retired
            ok=common and (declared if compiler_retirement is not None else not receipt["ForcedCleanup"])
            stages.append(dict(Name=name,Passed=ok,Process=receipt));need(ok,"Owned stage or physical cleanup failed: "+name);return receipt
        except Exception as error:errors.append(dict(Stage=name,Error=repr(error)));return None
    try:
        provider=load("async_provider",repo/"tools/Runtime/Erlang.OwnedGuest506Observations/collect.py")
        installed,erl,before,after=provider.provider(SimpleNamespace(official_admission=args.admission.resolve(),build_receipt=None,admission_sha256=sha(args.admission),otp_source=None),repo)
        save(out/"provider-before.json",before)
        package=args.package.resolve();pkgrows=package_inputs(package);packagebefore=sha(package)
        dotnet_found=shutil.which("dotnet.exe",path=env.get("PATH"));need(dotnet_found is not None,"Actual dotnet.exe absent from PATH")
        dotnet=bound_tool("dotnet",dotnet_found)
        cmd=bound_tool("cmd",Path(os.environ["SystemRoot"])/"System32/cmd.exe")
        fixtures=out/"fixtures";fixtures.mkdir()
        for name in ["async_msacc1127_drv.c","async_msacc1127.erl","async_cross_port1307.erl"]:shutil.copyfile(tool/"fixtures"/name,fixtures/name)
        header=next((installed/x for x in ["erts-17.1/include","usr/include"]if(installed/x/"erl_driver.h").is_file()),None);need(header is not None,"Admitted driver header missing")
        native_ready=False
        try:
            vs=bound_tool("vswhere",Path(os.environ["ProgramFiles(x86)"])/"Microsoft Visual Studio/Installer/vswhere.exe")
            found=stage("vswhere",[vs,"-latest","-products","*","-requires","Microsoft.VisualStudio.Component.VC.Tools.x86.x64","-property","installationPath"],timeout=30)
            if found:
                install=Path((out/"vswhere.stdout.log").read_text().strip());vcvars=bound_tool("vcvars",install/"VC/Auxiliary/Build/vcvars64.bat");need(vcvars.is_file(),"Actual VC setup absent")
                compiler=bound_tool("compiler",sorted((install/"VC/Tools/MSVC").glob("*/bin/Hostx64/x64/cl.exe"),reverse=True)[0])
                nativeargs=[str(compiler),"/nologo","/std:c11","/W4","/WX","/O2","/MD","/LD","/I"+str(header),str(fixtures/"async_msacc1127_drv.c"),"/Fe:"+str(fixtures/"async_msacc1127_drv.dll")]
                for item in [str(vcvars),*nativeargs]:need(not any(c in item for c in ['"','%','!','\r','\n','&','|','<','>','^']),"Unsafe generated native command path")
                batch=out/"compile-fixture.cmd";batch.write_text('@echo off\ncall "'+str(vcvars)+'" >nul\nif errorlevel 1 exit /b %errorlevel%\n'+' '.join('"'+x+'"'for x in nativeargs)+'\nexit /b %errorlevel%\n',encoding="utf-8")
                save(out/"native-inputs.json",dict(Compiler=dict(Path=str(compiler),Sha256=sha(compiler)),Vcvars=dict(Path=str(vcvars),Sha256=sha(vcvars)),CommandFileSha256=sha(batch),SourceSha256=sha(fixtures/"async_msacc1127_drv.c"),HeaderFiles=[dict(Path=str(p),Sha256=sha(p))for p in sorted(header.iterdir())if p.is_file()]))
                compiler_environment = dict(env)
                compiler_environment["VSCMD_SKIP_SENDTELEMETRY"] = "1"
                save(out/"compiler-environment-overrides.json", {"VSCMD_SKIP_SENDTELEMETRY":"1", "Scope":"Only fixture-native child environment; preserve actual Job descendant cleanup, source/compiler flags and deadlines"})
                ancillary=bound_tool("compiler-ancillary",compiler.with_name("vctip.exe"))
                compiler_retirement=dict(Version=1,CompilerPath=str(compiler),CompilerSha256=sha(compiler),AncillaryPath=str(ancillary),AncillarySha256=sha(ancillary),SourcePath=str(fixtures/"async_msacc1127_drv.c"),SourceSha256=sha(fixtures/"async_msacc1127_drv.c"),OutputPath=str(fixtures/"async_msacc1127_drv.dll"))
                save(out/"compiler-ancillary-profile.json",compiler_retirement)
                native_ready=stage("fixture-native",[cmd,"/d","/c",batch],cwd=fixtures,process_environment=compiler_environment,compiler_retirement=compiler_retirement) is not None
            if not found:skipped("fixture-native",["vswhere"])
        except Exception as error:
            errors.append(dict(Stage="fixture-native-setup",Error=repr(error)))
            if not any(row["Name"]=="fixture-native" for row in stages):skipped("fixture-native",["fixture-native-setup"])
        beam_ready=stage("fixture-beam",[installed/"bin/erlc.exe","+debug_info","-o",fixtures,fixtures/"async_msacc1127.erl"]) is not None
        queued_beam_ready=stage("fixture-queued-port-beam",[installed/"bin/erlc.exe","+debug_info","-o",fixtures,fixtures/"async_cross_port1307.erl"]) is not None
        original_ready={}
        for count in [1,2]:
            if native_ready and beam_ready:
                original_ready[count]=stage("original-A"+str(count),[erl,"+S","2:2","+A",str(count),"-noshell","-noinput","-pa",fixtures,"-s","async_msacc1127","main","-extra",fixtures,out/("original-A"+str(count)+".etf")],timeout=90) is not None
            else:
                original_ready[count]=False;skipped("original-A"+str(count),["fixture-native","fixture-beam"])
        if native_ready and queued_beam_ready:
            queued_original_ready=stage("original-queued-port-close-A1",[erl,"+S","2:2","+A","1","-noshell","-noinput","-pa",fixtures,"-s","async_cross_port1307","main","-extra",fixtures,out/"original-queued-port-close.etf"],timeout=90) is not None
        else:
            queued_original_ready=False
            skipped("original-queued-port-close-A1",["fixture-native","fixture-queued-port-beam"])
        unavailable={}
        if not native_ready:
            unavailable["component-child-reservation-rollback"]="Fixture native prerequisite failed"
        if not queued_original_ready:
            unavailable["original-queued-port-close-A1"]="Required original queued-port close or native/BEAM preparation failed"
        for count in [1,2]:
            if not original_ready[count]:unavailable["original-driver-A"+str(count)]="Prerequisite original-A"+str(count)+" failed or was blocked"
        if not(native_ready and beam_ready):
            for name in ["public-queued-ack-cancel","public-queued-ack-close",*PENDING_CASE_ROSTER]:unavailable[name]="Fixture native/BEAM prerequisite failed"
        project=tool/"Erlang.AsyncMicrostateValidation.csproj"
        built=stage("observer-build",[dotnet,"build",project,"-c",args.configuration,"-m:1","-nodeReuse:false","-p:UseSharedCompilation=false","-p:ReviewedPackageRoot="+str(package.parent),"--nologo"],cwd=repo,timeout=120)
        if built is None:skipped("observer-execute",["observer-build"])
        need(built is not None,"Observation build failed; refuse stale executable launch")
        dll=tool/"bin"/args.configuration/"net10.0/Erlang.AsyncMicrostateValidation.dll"
        observer_inputs = [dict(Path=str(p), Sha256=sha(p)) for p in sorted(dll.parent.rglob("*")) if p.is_file()]
        need({str(dll),str(dll.with_suffix(".deps.json")),str(dll.with_suffix(".runtimeconfig.json"))} <= {row["Path"] for row in observer_inputs}, "Actual observer DLL/deps/runtimeconfig closure absent")
        save(out/"observer-executable-inputs.json", observer_inputs)
        guards={str(repo/r["Path"]):sha(repo/r["Path"])for r in plan["Inputs"]};guards.update({r["Path"]:r["Sha256"]for r in pkgrows});guards[str(package)]=packagebefore
        guards.update({row["Path"]:row["Sha256"] for row in observer_inputs})
        guards.update({row["Path"]:row["Sha256"] for row in execution_tools.values()})
        save(out/"execution-tools.json",execution_tools)
        guards[str(args.admission.resolve())]=sha(args.admission)
        guards[str(before["RelatedAdmissions"][0]["Path"])]=before["RelatedAdmissions"][0]["Sha256"]
        for p in fixtures.iterdir():
            if p.is_file():guards[str(p)]=sha(p)
        for count in [1,2]:
            p=out/("original-A"+str(count)+".etf")
            if p.exists():guards[str(p)]=sha(p)
        queued_original=out/"original-queued-port-close.etf"
        if queued_original.is_file():guards[str(queued_original)]=sha(queued_original)
        setting=dict(QueuedPortCloseBeam=str(fixtures/"async_cross_port1307.beam"),OriginalQueuedPortClose=str(queued_original),QueuedPortCloseOwnEtf=str(out/"own-queued-port-close.etf"),UnavailableCases=unavailable,ObserverAssemblyPath=str(dll),PackageManifest=str(package),DdllBeam=str(repo/"src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin/erl_ddll.beam"),ObserverBeam=str(fixtures/"async_msacc1127.beam"),DriverDirectory=str(fixtures),OriginalA1=str(out/"original-A1.etf"),OriginalA2=str(out/"original-A2.etf"),Configuration=args.configuration,Guards=[dict(Path=p,Sha256=h)for p,h in sorted(guards.items())],AssemblyGuards=[dict(Name=f,Sha256=sha(package.parent/f))for f in ["Erlang.Runtime.dll","Erlang.Terms.dll","Erlang.Serialization.dll","Erlang.Otp.Stdlib.dll"]])
        save(out/"inputs.json",setting)
        stage("observer-execute",[dotnet,"exec","--fx-version","10.0.11","--roll-forward","Disable",dll,out/"inputs.json",out/"observations.json"],cwd=repo,timeout=300)
        observation=json.loads((out/"observations.json").read_bytes());need(observation["Passed"] and [c["Name"] for c in observation["Cases"]] == list(EXPECTED_CASE_ROSTER) and all(c["Passed"]for c in observation["Cases"]) and not observation["Errors"],"Mandatory independent23 observations failed")
    except Exception as error:errors.append(dict(Stage="composition",Error=repr(error)))
    finally:
        for name,row in execution_tools.items():
            try:need(sha(row["Path"])==row["Sha256"] and Path(row["Path"]).stat().st_size==row["Bytes"],"Execution tool changed: "+name)
            except Exception as error:errors.append(dict(Stage="execution-tool-final",Name=name,Error=repr(error)))
        if observer_inputs is not None:
            try:
                need({str(p) for p in dll.parent.rglob("*") if p.is_file()} == {row["Path"] for row in observer_inputs}, "Observer executable membership changed")
                for row in observer_inputs:need(sha(row["Path"]) == row["Sha256"], "Observer executable input changed")
            except Exception as error:errors.append(dict(Stage="observer-executable-final",Error=repr(error)))
        if packagebefore is not None:
            try:
                need(sha(package)==packagebefore,"Package manifest changed");package_inputs(package)
            except Exception as error:errors.append(dict(Stage="package-final",Error=repr(error)))
        if after is not None:
            try:
                final=after();save(out/"provider-after.json",final);need(final==before["Inventory"],"Original provider changed")
            except Exception as error:errors.append(dict(Stage="provider-final",Error=repr(error)))
        try:sealed(plan,repo)
        except Exception as error:errors.append(dict(Stage="source-final",Error=repr(error)))
        save(out/"source-inputs.json",sourcebefore)
        save(out/"completion.json",dict(Passed=not errors,Configuration=args.configuration,PlanSha256=args.plan_sha256,ExecutionTools=execution_tools,Stages=stages,Errors=errors,Scope="Actual Windows child async; same original native fixture,23 independently retained cases: original8, eight pending-RPC public phase/cause cases, five actual gate controls and two unpublished no-op release cases. No raw topology/timing/fullstatistics, mid-native-body cancellation or Linux claim"))
    if errors:
        print(json.dumps(dict(AsyncMicrostateFailure=True,Errors=errors),ensure_ascii=True)[:16384],flush=True)
        observation_file=out/"observations.json"
        if observation_file.is_file():
            try:
                failed_observation=json.loads(observation_file.read_bytes())
                print(json.dumps(dict(ObservationErrors=failed_observation.get("Errors",[]),FailedCases=[case for case in failed_observation.get("Cases",[]) if not case.get("Passed",False)]),ensure_ascii=True)[:16384],flush=True)
            except Exception as summary_error:
                print("Cannot read retained observation summary: "+repr(summary_error),flush=True)
        raise RuntimeError("Strict async microstate observation failed; all independent outcomes retained")
if __name__=="__main__":main()
