"""Source-bound fresh original monitor645 compile/execute + exact managed ten-row replay."""
import argparse, base64, hashlib, importlib.util, json, os, re, shutil, sys, time
from pathlib import Path
sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
CASE = 'runtime/monitor645/original-public-role-whole-outcomes'
SCHEMA = 'monitor641/public-role-observer-v1'
COUNT = 10

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def need(value, message):
    if not value: raise RuntimeError(message)
def save(path, value):
    with path.open('x', encoding='utf-8') as stream: json.dump(value, stream, indent=2)
def load(name, path):
    spec = importlib.util.spec_from_file_location(name,path); module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
def estr(value): return '"' + str(value).replace('\\','/').replace('"','\\"') + '"'
def guard(a, repo, tool):
    plan=json.loads((tool/'inputs.json').read_bytes());need(plan['Pin']==PIN and plan['Count']==COUNT,'Wrong protocol')
    need(sha(tool/'run.py')==plan['RunnerSha256'],'Runner changed')
    for row in plan['Inputs']: need(sha(repo/row['Path'])==row['Sha256'],'Reviewed source/helper changed: '+row['Path'])
    fixture=tool/'fixtures/monitor_original645.erl';need(sha(fixture)==plan['ObserverSha256'],'Observer changed')
    names=(repo/plan['Names']).read_text();match=re.search(r'const string ObserverBase64 = "([^"]+)"',names)
    need(match is not None and base64.b64decode(match.group(1))==fixture.read_bytes(),'Managed source differs')
    need(sha(a.source_freeze)==a.source_freeze_sha256.upper(),'Final source-freeze changed')
    frozen=json.loads(a.source_freeze.read_bytes());need(bool(frozen['Rows']),'Empty source freeze')
    for row in frozen['Rows']:need(sha(repo/row['Path'])==row['Sha256'],'Final source differs: '+row['Path'])
    return plan

def original(a,repo,tool,out,owned):
    d=out/'original';d.mkdir();errors=[];before=None;after=None;passed=False
    try:
        collect=load('monitor645_provider',repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py');a.build_receipt=None;a.otp_source=None
        installed,erl,before,after=collect.provider(a,repo);save(d/'provider-before.json',before)
        source=tool/'fixtures/monitor_original645.erl';shutil.copyfile(source,d/source.name)
        env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',ERL_CRASH_DUMP=str(d/'erl_crash.dump'))
        for key in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR','ERLC_USE_SERVER','ERLC_SERVER_ID','ERLC_CONFIGURATION','ERL_ROOTDIR']:env.pop(key,None)
        env['ERL_FLAGS']='+S 1:1 +A 1';erlc=installed/'bin/erlc.exe';need(erlc.is_file(),'Original compiler absent')
        owned.owned([str(erlc),'+debug_info','-o',str(d),str(d/source.name)],d,'compile',120,env,str(d))
        expr=('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
            'Root=filename:absname('+estr(installed)+'),Root=filename:absname(code:root_dir()),'
            '8=erlang:system_info(wordsize),1=erlang:system_info(schedulers_online),'
            'W("profile.etf",{erlang:system_info(wordsize),erlang:system_info(schedulers),erlang:system_info(schedulers_online),erlang:system_info(debug_compiled),erlang:system_info(otp_release)}),'
            'monitor_original645:module_info(),P=filename:absname("monitor_original645.beam"),P=filename:absname(code:which(monitor_original645)),{ok,{monitor_original645,D}}=beam_lib:md5(P),D=monitor_original645:module_info(md5),'
            'W("observer-owner.etf",{P,D,monitor_original645:module_info(compile)}),'
            'ok=monitor_original645:capture('+estr(d)+'),halt(0).')
        (d/'invocation.eval').write_text(expr,encoding='utf-8')
        owned.owned([str(erl),'+S','1:1','+A','1','-noshell','-noinput','-boot','no_dot_erlang','-pa',str(d),'-eval',expr],d,'original',180,env,str(d))
        passed=True
    except Exception as error:errors.append(repr(error))
    finally:
        if after is not None:
            try:
                current=after();save(d/'provider-after.json',dict(Inventory=current,ProviderEqual=current==before['Inventory']));need(current==before['Inventory'],'Provider changed')
            except Exception as error:errors.append('postflight: '+repr(error))
        source=tool/'fixtures/monitor_original645.erl'
        if (d/source.name).exists() and sha(d/source.name)!=sha(source):errors.append('Copied observer changed')
    result=dict(Passed=passed and not errors,Errors=errors,FreshOriginalCompilation=(json.loads((d/'compile.exit.json').read_bytes()).get('ExitCode')==0) if (d/'compile.exit.json').exists() else False,OriginalCompilationAttempted=(d/'compile.live.json').exists(),FreshOriginalExecution=(d/'original.live.json').exists(),RawRowsComplete=all((d/f'outcome-{i}.etf').is_file() for i in range(COUNT)),OriginalPhysicalProof=False)
    save(d/'completion.json',result);return result

def managed(a,repo,out,owned):
    d=out/'managed';d.mkdir();errors=[];host=repo/'tests/Erlang.Tests/bin'/a.configuration/'net10.0/Erlang.Tests.dll';binary=host.parent;base=repo/'artifacts/monitor645-managed';before=set(base.iterdir()) if base.exists() else set();started=time.time_ns();inventory={};fresh=None
    def dlls():return {q.name:sha(q) for q in sorted(binary.iterdir()) if q.is_file() and (q.suffix.lower()=='.dll' or q.name.endswith('.deps.json') or q.name.endswith('.runtimeconfig.json'))}
    try:
        need(host.is_file(),'Managed host absent');inventory=dlls()
        owned.owned(['dotnet','exec','--fx-version',a.framework_version,str(host),'--case',CASE,'--report',str(d/'report.json')],d,'managed',240,dict(os.environ,DOTNET_PROCESSOR_COUNT='2',PYTHONDONTWRITEBYTECODE='1'),str(repo))
    except Exception as error:errors.append(repr(error))
    completed=time.time_ns()
    # Preserve independent captures even when the named managed case failed.
    try:
        added=(set(base.iterdir()) if base.exists() else set())-before;need(len(added)==1,'One fresh managed GUID directory required');fresh=added.pop();need(fresh.is_dir() and re.fullmatch('[0-9a-fA-F]{32}',fresh.name),'Invalid GUID output')
        for q in fresh.iterdir():
            if q.is_file():
                need(started<=q.stat().st_mtime_ns<=completed,'Managed capture outside owned window: '+q.name);shutil.copyfile(q,d/q.name)
    except Exception as error:errors.append('capture: '+repr(error))
    try:
        need(inventory and dlls()==inventory,'Managed DLL/config inventory changed')
        report=json.loads((d/'report.json').read_bytes());need(report['Passed']==1 and report['Failed']==0 and report['SelectedCount']==1 and len(report['Tests'])==1 and report['Tests'][0]['Name']==CASE and report['Tests'][0]['Status']=='Passed','Exact managed case failed')
        settled=json.loads((d/'managed.exit.json').read_bytes());need(settled['Settled'] and settled['ExitCode']==0 and not settled['TimedOut'] and not settled['Failures'],'Managed child not settled')
        receipt=json.loads((d/'receipt.json').read_bytes());need(receipt['Passed'] and receipt['Schema']==SCHEMA and receipt['OriginalPin']==PIN and receipt['ExpectedRows']==COUNT and receipt['CapturedRows']==COUNT and not receipt['Errors'],'Managed capture/cleanup failed')
        need(receipt['SourceBytesEqual'] and receipt['DllsUnchanged'] and len(receipt['FamilyCleanupCompleted'])==5,'Managed cleanup/source proof incomplete')
        need(receipt['HostSha256']==inventory['Erlang.Tests.dll'] and receipt['RuntimeSha256']==inventory['Erlang.Runtime.dll'],'Managed capture DLL identity differs')
        need(not receipt['FrameProjection'] and not receipt['RawOutcomesRewritten'] and not receipt['NativeNifCompared'] and not receipt['OrdinaryReferenceIsNativeResource'],'Unexpected projection/native credit')
    except Exception as error:errors.append('postflight: '+repr(error))
    result=dict(Passed=not errors,Errors=errors,Case=CASE,StartedUnixNs=started,CompletedUnixNs=completed,FreshOutput=str(fresh) if fresh else None,ProtectedDllAndConfigBefore=inventory,RawRowsComplete=all((d/f'outcome-{i}.etf').is_file() for i in range(COUNT)))
    save(d/'completion.json',result);return result

def main():
    p=argparse.ArgumentParser();p.add_argument('--repository',type=Path,required=True);p.add_argument('--official-admission',type=Path,required=True);p.add_argument('--admission-sha256',required=True);p.add_argument('--source-freeze',type=Path,required=True);p.add_argument('--source-freeze-sha256',required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--configuration',choices=['Debug','Release'],default='Debug');p.add_argument('--framework-version',default='10.0.11');a=p.parse_args();repo=a.repository.resolve();tool=Path(__file__).resolve().parent;out=a.output.resolve()
    need(os.name=='nt' and out.is_relative_to(repo/'artifacts') and not out.exists(),'Fresh confined output required');guard(a,repo,tool);out.mkdir(parents=True)
    owned=load('monitor645_owned',repo/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py')
    o=original(a,repo,tool,out,owned);m=managed(a,repo,out,owned);errors=[];equal={}
    if o['RawRowsComplete'] and m['RawRowsComplete']:
        for i in range(COUNT):
            name=f'outcome-{i}.etf';equal[name]=(out/'original'/name).read_bytes()==(out/'managed'/name).read_bytes()
    else:errors.append('Whole-row comparison requires all ten actual captures on both routes')
    try:guard(a,repo,tool)
    except Exception as error:errors.append('final-guard: '+repr(error))
    result=dict(Passed=o['Passed'] and m['Passed'] and len(equal)==COUNT and all(equal.values()) and not errors,Original=o,Managed=m,CompleteRawOutcomeEqual=equal,EqualRows=sum(equal.values()),ExpectedRows=COUNT,Errors=errors,FrameProjection=False,RawEtfRewritten=False,SourceFreezeSha256=a.source_freeze_sha256.upper(),BuildSourceVerifiedByCollector=False,NativeNifCompared=False,OrdinaryReferenceIsNativeResource=False,OriginalPhysicalProof=False,ApiDelta=0,FullContractDelta=0,Files={q.relative_to(out).as_posix():sha(q) for q in out.rglob('*') if q.is_file()})
    save(out/'completion.json',result);return 0 if result['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
