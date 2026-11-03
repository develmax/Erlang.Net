"""Reproducible fresh original dictionary537 and one exact managed replay; no builds/golden rewrites."""
import argparse,base64,hashlib,importlib.util,json,os,re,shutil,sys,time
from pathlib import Path
sys.dont_write_bytecode=True
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
CASE='runtime/dictionary627/exact-original-observer-whole-outcome'

def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def need(v,m):
    if not v:raise RuntimeError(m)
def save(p,v):
    with p.open('x',encoding='utf-8') as f:json.dump(v,f,indent=2)
def load(n,p):
    s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def erlstr(s):return '"'+str(s).replace('\\','/').replace('"','\\"')+'"'
def preflight(repo,tool):
    plan=json.loads((tool/'inputs.json').read_bytes());need(plan['Pin']==PIN,'Wrong pin')
    for row in plan['Inputs']:need(sha(repo/row['Path'])==row['Sha256'],'Reviewed input differs: '+row['Path'])
    for name,h in plan['Fixtures'].items():need(sha(tool/'fixtures'/name)==h,'Fixture differs: '+name)
    text=(repo/'tests/Erlang.Tests/Runtime/Storage/Dictionary/DictionaryOriginal631Names.cs').read_text()
    for k,n in [('SourceBase64','dictionary_keys605.erl'),('CaptureBase64','dictionary_keys605_capture631.erl')]:
        found=re.search(r'const string '+k+r' = "([^"]+)"',text);need(found is not None and base64.b64decode(found.group(1))==(tool/'fixtures'/n).read_bytes(),'Managed embedded source differs: '+k)
    return plan

def original(a,repo,tool,out,owned):
    d=out/'original';d.mkdir();errors=[];before=None;after=None;passed=False
    try:
        collect=load('dictionary537_collect',repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py');a.build_receipt=None;a.otp_source=None
        installed,erl,before,after=collect.provider(a,repo);save(d/'provider-before.json',before)
        erlc=installed/'bin/erlc.exe';need(erlc.is_file(),'Admitted compiler missing')
        for q in (tool/'fixtures').glob('*.erl'):shutil.copyfile(q,d/q.name)
        env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',ERL_CRASH_DUMP=str(d/'erl_crash.dump'))
        for k in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR','ERLC_USE_SERVER','ERLC_SERVER_ID','ERLC_CONFIGURATION','ERL_ROOTDIR']:env.pop(k,None)
        env['ERL_FLAGS']='+S 1:1 +A 1'
        # Runner is dependent on successful genuine compilation; never continue after compiler failure.
        owned.owned([str(erlc),'+debug_info','-o',str(d),str(d/'dictionary_keys605.erl'),str(d/'dictionary_keys605_capture631.erl')],d,'compile',120,env,str(d))
        expr=('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
              'Expected=filename:absname('+erlstr(installed)+'),Expected=filename:absname(code:root_dir()),'
              '8=erlang:system_info(wordsize),1=erlang:system_info(schedulers_online),'
              'W("profile.etf",{erlang:system_info(wordsize),erlang:system_info(schedulers),erlang:system_info(schedulers_online),erlang:system_info(debug_compiled),erlang:system_info(otp_release)}),'
              '[begin M:module_info(),P=filename:absname(atom_to_list(M)++".beam"),P=filename:absname(code:which(M)),{ok,{M,D}}=beam_lib:md5(P),D=M:module_info(md5),W(atom_to_list(M)++"-owner.etf",{M,P,D,M:module_info(compile)}) end||M<-[dictionary_keys605,dictionary_keys605_capture631]],'
              'atomics:module_info(),W("atomics-owner.etf",{code:which(atomics),atomics:module_info(md5),atomics:module_info(compile)}),'
              'Outcome=dictionary_keys605_capture631:run(),W("observed.etf",Outcome),io:format("~tp~n",[Outcome]),case Outcome of {ok,_}->halt(0);_->halt(1) end.')
        (d/'invocation.eval').write_text(expr,encoding='utf-8')
        owned.owned([str(erl),'+S','1:1','+A','1','-noshell','-noinput','-boot','no_dot_erlang','-pa',str(d),'-eval',expr],d,'original',60,env,str(d))
        need((d/'observed.etf').is_file(),'Original whole Outcome absent');passed=True
        save(d/'executables.json',{'Compiler':{'Path':str(erlc),'Sha256':sha(erlc)},'Runtime':{'Path':str(erl),'Sha256':sha(erl)}})
    except Exception as e:errors.append(repr(e))
    finally:
        if after is not None:
            try:
                now=after();save(d/'provider-after.json',{'ProviderEqual':now==before['Inventory'],'Inventory':now});need(now==before['Inventory'],'Original provider changed')
            except Exception as e:errors.append('postflight: '+repr(e))
        for q in (tool/'fixtures').glob('*.erl'):
            if (d/q.name).exists() and sha(d/q.name)!=sha(q):errors.append('Copied source differs: '+q.name)
        result={'Passed':passed and not errors,'Errors':errors,'FreshOriginalCompilation':(d/'compile.live.json').exists(),'FreshOriginalExecution':(d/'original.live.json').exists(),'RawEtfRewritten':False,'FrameProjection':False,'SourcePin':PIN,'Files':{q.name:sha(q) for q in d.iterdir() if q.is_file()}}
        save(d/'completion.json',result)
    return result

def managed(a,repo,tool,out,owned):
    d=out/'managed';d.mkdir();errors=[];started=time.time_ns();host=repo/'tests/Erlang.Tests/bin'/a.configuration/'net10.0/Erlang.Tests.dll';runtime=host.with_name('Erlang.Runtime.dll');base=repo/'artifacts/dictionary605-managed631';before=set(base.iterdir()) if base.exists() else set();frame={'Passed':False,'Errors':errors,'Configuration':a.configuration,'Case':CASE,'StartedUnixNs':started};hashes={}
    try:
        for n,q in [('Host',host),('Runtime',runtime)]:need(q.is_file(),'Missing managed '+n);hashes[n]=sha(q)
        frame['DllBefore']=hashes
        env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',PYTHONDONTWRITEBYTECODE='1')
        owned.owned(['dotnet','exec','--fx-version',a.framework_version,str(host),'--case',CASE,'--report',str(d/'report.json')],d,'managed',90,env,str(repo))
    except Exception as e:errors.append(repr(e))
    finally:
        try:
            frame['CompletedUnixNs']=time.time_ns()
            for n,q in [('Host',host),('Runtime',runtime)]:need(n in hashes and sha(q)==hashes[n],'Managed DLL changed: '+n)
            report=json.loads((d/'report.json').read_bytes());need(report['Passed']==1 and report['Failed']==0 and report['SelectedCount']==1 and len(report['Tests'])==1 and report['Tests'][0]['Name']==CASE and report['Tests'][0]['Status']=='Passed','Exact managed case failed')
            receipt=json.loads((d/'managed.exit.json').read_bytes());need(receipt['Settled'] and receipt['ExitCode']==0 and not receipt['TimedOut'] and not receipt['Failures'],'Managed child did not settle successfully')
            added=set(base.iterdir())-before;need(len(added)==1,'Expected exactly one fresh managed output directory');fresh=added.pop();need(fresh.is_dir(),'Fresh output is not directory');proof=json.loads((fresh/'receipt.json').read_bytes())
            need(proof['Passed'] and proof['WholeOutcomeCaptured'] and proof['ObserverSourceBytesEqual'] and proof['ActorDone'] and proof['RuntimeDisposeCompleted'],'Managed actual cleanup/source/capture failed')
            need(proof['RuntimeSha256']==hashes['Runtime'] and proof['HostSha256']==hashes['Host'],'Captured DLL identity differs')
            need(not proof['RawEtfRewritten'] and not proof['FrameProjection'] and not proof['GoldenCompared'],'Managed result was transformed')
            need(proof['ObserverSourceSha256']==sha(tool/'fixtures/dictionary_keys605.erl'),'Managed observer differs')
            need(proof['CaptureSourceSha256']==sha(tool/'fixtures/dictionary_keys605_capture631.erl'),'Managed capture worker differs')
            for n in ['observed.etf','receipt.json','dictionary_keys605.erl','dictionary_keys605_capture631.erl']:
                q=fresh/n;need(q.is_file() and started<=q.stat().st_mtime_ns<=frame['CompletedUnixNs'],'Managed file outside owned window: '+n);shutil.copyfile(q,d/n)
            need(sha(d/'observed.etf')==proof['ActualSha256'],'Captured managed whole bytes differ');frame['FreshOutput']=str(fresh);frame['ProtectedFiles']={q.name:sha(q) for q in d.iterdir() if q.is_file()}
        except Exception as e:errors.append('postflight: '+repr(e))
        frame['Passed']=not errors;save(d/'frame.json',frame)
    return frame

def main():
    p=argparse.ArgumentParser();p.add_argument('--repository',type=Path,required=True);p.add_argument('--official-admission',type=Path,required=True);p.add_argument('--admission-sha256',required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--configuration',choices=['Debug','Release'],default='Debug');p.add_argument('--framework-version',default='10.0.11');a=p.parse_args();repo=a.repository.resolve();tool=Path(__file__).resolve().parent;out=a.output.resolve()
    need(os.name=='nt' and out.is_relative_to(repo/'artifacts') and not out.exists(),'Fresh confined Windows artifacts output required');preflight(repo,tool);out.mkdir(parents=True)
    owned=load('dictionary537_owned',repo/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py')
    # Independent original and managed routes always both run; comparison is dependent on BOTH successes.
    o=original(a,repo,tool,out,owned);m=managed(a,repo,tool,out,owned);errors=[];equal=False
    if o['Passed'] and m['Passed']:
        left=(out/'original/observed.etf').read_bytes();right=(out/'managed/observed.etf').read_bytes();equal=left==right
        if not equal:errors.append('Complete original/managed Outcome bytes differ')
        if len(left)!=108 or len(right)!=108:errors.append('Expected actual108-byte current observer Outcome')
    else:errors.append('Whole comparison skipped because prerequisite route failed')
    try:preflight(repo,tool)
    except Exception as e:errors.append('final-source-guard: '+repr(e))
    result={'Passed':o['Passed'] and m['Passed'] and equal and not errors,'OriginalPassed':o['Passed'],'ManagedPassed':m['Passed'],'WholeRawEqual':equal,'OutcomeBytes':108 if equal and not errors else None,'Errors':errors,'Projection':False,'GoldenRewritten':False,'ApiDelta':0,'FullContractDelta':0,'Files':{q.relative_to(out).as_posix():sha(q) for q in out.rglob('*') if q.is_file()}}
    save(out/'completion.json',result);return 0 if result['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
