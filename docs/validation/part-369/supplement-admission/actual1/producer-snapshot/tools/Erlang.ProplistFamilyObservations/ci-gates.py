"""Bounded, source-admitted Linux preflight; never builds or invokes OTP."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,subprocess,threading,time
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(p,n):
    s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def main():
    p=argparse.ArgumentParser();p.add_argument('phase',choices=['admit','parse']);p.add_argument('--head',required=True);p.add_argument('--manifest-sha256',required=True);a=p.parse_args()
    root=Path.cwd().resolve();tool=root/'tools/Erlang.ProplistFamilyObservations';manifest=tool/'source-inputs.corrected.json'
    assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==a.head==os.environ['GITHUB_SHA']
    assert sha(manifest)==a.manifest_sha256.upper();plan=json.loads(manifest.read_bytes())
    for key,base in [('RepositoryInputs',root),('ToolInputs',tool)]:
        for row in plan[key]:
            f=(base/row['Path']).resolve();assert f.is_relative_to(base) and f.stat().st_size==row['Bytes'] and sha(f)==row['Sha256'],row['Path']
    shared=load(root/'tools/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','shared_proplists22')
    shared.verify(root,plan['ExistingBuildManifestSha256'],a.head)
    out=root/'artifacts/proplists22-original/preflight';out.mkdir(parents=True,exist_ok=True)
    if a.phase=='admit':
        (out/'source-admission.json').write_text(json.dumps(dict(Status='PASS_SOURCE_ADMISSION',Head=a.head,ManifestSha256=sha(manifest)),indent=2)+'\n');return
    owned=load(root/'tools/Erlang.GuestConnectedSendObservations/owned_process.py','owned_proplists22')
    env=shared.sanitized_environment(os.environ);env.update(DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    for key in ['GH_TOKEN','GITHUB_TOKEN','LD_PRELOAD','LD_LIBRARY_PATH']:env.pop(key,None)
    stages=[]
    def run(name,args,seconds):
        process=None;failure=None
        try:
            process=owned.OwnedProcess([str(x) for x in args],root,env,out/name)
            process.wait(time.monotonic()+seconds,threading.Event().is_set)
        except BaseException as error:failure=repr(error)
        finally:
            if process is not None:
                try:process.close()
                except BaseException as error:failure=(failure or '')+'; cleanup: '+repr(error)
            receipt=out/(name+'.process.json')
            if receipt.exists():
                if failure is None:
                    try:shared.require_normal_receipt(json.loads(receipt.read_bytes()))
                    except BaseException as error:failure=repr(error)
            else:failure=failure or 'Missing owned process receipt'
            stages.append(dict(Name=name,Arguments=[str(x) for x in args],Failure=failure,ReceiptSha256=sha(receipt) if receipt.exists() else None))
            (out/'completion.json').write_text(json.dumps(dict(Status='FAIL_LOCAL_GATE' if failure else 'STAGES_ACCEPTED',Stages=stages,OriginalExecuted=False),indent=2)+'\n')
        if failure:raise RuntimeError(name+' failed; raw output retained')
    run('parse-build',['dotnet','build',tool/'parse-gate/ProplistFamily.ParseGate.csproj','--disable-build-servers','-m:1','-nr:false','--nologo','-p:UseSharedCompilation=false','-p:NuGetAudit=false'],180)
    gate=out/'parse-receipt.json'
    run('parse-sources',['dotnet',tool/'parse-gate/bin/Debug/net10.0/ProplistFamily.ParseGate.dll',root,tool,gate],30)
    run('collector-admit',['python3','-B',tool/'collect-original-corrected.py','--repository',root,'--expected-head',a.head,'--source-inputs-sha256',sha(manifest),'--parse-gate',gate,'--parse-gate-sha256',sha(gate),'--verify-only'],30)
    (out/'completion.json').write_text(json.dumps(dict(Status='PASS_LINUX_NAMED_FIXTURE_GATE',Stages=stages,ParseGateSha256=sha(gate),OriginalExecuted=False),indent=2)+'\n')
if __name__=='__main__':main()
