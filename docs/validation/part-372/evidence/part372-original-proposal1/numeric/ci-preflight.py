"""Owned independent Part372 preflight, one source-admitted adapter, no OTP execution."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,threading,time
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def load(p,n):
    s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def main():
    p=argparse.ArgumentParser();p.add_argument('--head',required=True);p.add_argument('--manifest-sha256',required=True);a=p.parse_args()
    root=Path.cwd().resolve();tool=Path(__file__).resolve().parent;manifest=tool/'source-inputs.corrected.json';assert H(manifest)==a.manifest_sha256.upper()
    plan=json.loads(manifest.read_bytes())
    for key,base in [('RepositoryInputs',root),('ToolInputs',tool)]:
        for row in plan[key]:
            q=(base/row['Path']).resolve();assert q.is_relative_to(base) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    shared=load(root/'tools/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','ordsets371_shared');shared.verify(root,plan['ExistingBuildManifestSha256'],a.head)
    owned=load(root/'tools/Erlang.GuestConnectedSendObservations/owned_process.py','ordsets371_owned')
    out=root/'artifacts/part372-original'/('ordsets-preflight' if tool.name=='Erlang.Ordsets372OriginalObservations' else 'numeric-preflight');out.mkdir(parents=True,exist_ok=False)
    env=shared.sanitized_environment(os.environ);env.update(DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    for key in ['GH_TOKEN','GITHUB_TOKEN','LD_PRELOAD','LD_LIBRARY_PATH']:env.pop(key,None)
    stages=[]
    def run(name,args,seconds):
        process=None;failure=None
        try:
            process=owned.OwnedProcess([str(x) for x in args],root,env,out/name);process.wait(time.monotonic()+seconds,threading.Event().is_set)
        except BaseException as e:failure=repr(e)
        finally:
            if process is not None:
                try:process.close()
                except BaseException as e:failure=(failure or '')+';cleanup:'+repr(e)
            receipt=out/(name+'.process.json')
            if receipt.exists():
                if failure is None:
                    try:shared.require_normal_receipt(json.loads(receipt.read_bytes()))
                    except BaseException as e:failure=repr(e)
            else:failure=failure or 'Missing receipt'
            stages.append(dict(Name=name,Arguments=[str(x) for x in args],Failure=failure,ReceiptSha256=H(receipt) if receipt.exists() else None))
            (out/'completion.json').write_text(json.dumps(dict(Status='FAIL_LOCAL_GATE' if failure else 'STAGES_ACCEPTED',Stages=stages,OriginalExecuted=False),indent=2)+'\n')
        if failure:raise RuntimeError(name+' failed, dependent phases skipped')
    run('parse-build',['dotnet','build',tool/'parse-gate/ProplistFamily.ParseGate.csproj','--disable-build-servers','-m:1','-nr:false','--nologo','-p:UseSharedCompilation=false','-p:NuGetAudit=false'],180)
    gate=out/'parse-receipt.json'
    run('parse-sources',['dotnet',tool/'parse-gate/bin/Debug/net10.0/ProplistFamily.ParseGate.dll',root,tool,gate],30)
    run('collector-admit',['python3','-B',tool/'collect-original-corrected.py','--repository',root,'--expected-head',a.head,'--source-inputs-sha256',H(manifest),'--parse-gate',gate,'--parse-gate-sha256',H(gate),'--verify-only'],30)
    (out/'completion.json').write_text(json.dumps(dict(Status='PASS_LINUX_NAMED_FIXTURE_GATE',Stages=stages,ParseGateSha256=H(gate),OriginalExecuted=False),indent=2)+'\n')
if __name__=='__main__':main()
