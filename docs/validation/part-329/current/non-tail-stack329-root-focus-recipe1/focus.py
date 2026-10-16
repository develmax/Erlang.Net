"""Root-only exact-frozen-candidate formatting/build/independent selector diagnostics."""
from pathlib import Path
import argparse,hashlib,json,os,re,subprocess,time,traceback
R=Path.cwd().resolve();HERE=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
write=lambda p,v:p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
p=argparse.ArgumentParser();p.add_argument('--candidate-directory',required=True,type=Path);p.add_argument('--expected-freeze',required=True);p.add_argument('--focusname',required=True);p.add_argument('--mode',choices=['diagnostic','expected-red','expected-green'],default='diagnostic');a=p.parse_args()
assert re.fullmatch('[A-Fa-f0-9]{64}',a.expected_freeze) and re.fullmatch('[a-z0-9][a-z0-9-]{0,90}',a.focusname)
AUTHOR=a.candidate_directory.resolve();assert AUTHOR.is_relative_to(R/'artifacts')
F=AUTHOR/'source-freeze.json';assert sha(F)==a.expected_freeze.upper();f=read(F)
closure=AUTHOR/'source-resource-closure.json';closurehash=f.get('FinalSourceClosureSha256',f.get('SourceClosureSha256'));assert closurehash and sha(closure)==closurehash
rows=read(closure)['Files'];ownedrows=f['Files'];owned={e['Path'] for e in ownedrows}
assert len(rows)==f['SourceClosurePaths'] and len(ownedrows)==len(owned)==f['SourcePaths']==f['CSharpPaths'] and all(n.endswith('.cs') for n in owned)
assert len({e['Path'] for e in rows})==len(rows) and owned<={e['Path'] for e in rows}
selectors=f.get('AllFocusSelectors', f['Selectors']);assert selectors and len(set(selectors))==len(selectors) and all(re.fullmatch(r'[A-Za-z_][A-Za-z0-9_.]*:[A-Za-z_][A-Za-z0-9_]*',s) for s in selectors)
assert a.mode != 'expected-red', 'Historical semantic RED requires separate explicit discriminator; no failure is automatically accepted'
refs=read(HERE/'references.json')
def verify_refs():
    for e in refs:
        q=R/e['Path'];assert q.stat().st_size==e['Bytes'] and sha(q)==e['Sha256']
    for e in read(HERE/'proposal-freeze.json')['Files']:
        q=HERE/e['Path'];assert q.stat().st_size==e['Bytes'] and sha(q)==e['Sha256']
def exact(base,records):
    for e in records:
        name=Path(e['Path']);assert not name.is_absolute() and '..' not in name.parts
        q=base/name;assert q.stat().st_size==e['Bytes'] and sha(q)==e['Sha256'],e['Path']
verify_refs();exact(AUTHOR/'candidate',rows)
assert all(all(next(e for e in rows if e['Path']==x['Path'])[k]==x[k] for k in ['Bytes','Sha256']) for x in ownedrows)
D=R/'artifacts'/a.focusname;assert not D.exists() and D!=AUTHOR and not AUTHOR.is_relative_to(D);D.mkdir();C=D/'candidate';C.mkdir();M=D/'mini'
for e in rows:
    q=C/e['Path'];q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes((AUTHOR/'candidate'/e['Path']).read_bytes())
for name in ['src','tools','tests','benchmarks','examples']:(M/name).mkdir(parents=True)
inputs=[str(C/n) for n in sorted(owned)];write(D/'token-inputs.json',inputs);write(D/'preformat-source-freeze.json',f);write(D/'preformat-source-resource-closure.json',{'Files':rows})
token=R/refs[0]['Path'];style=R/refs[1]['Path'];focus=R/refs[2]['Path'];env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_USE_MSBUILD_SERVER='0',MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false',RestoreDisableParallel='true')
def run(label,command,limit,validator,sourcecheck):
    out=D/label;assert not out.exists();out.mkdir();start=time.monotonic();expired=False;settled=False;cleanup={};proc=None;stdout=b'';stderr=b'';failure=None;classification='INFRASTRUCTURE_OR_RECEIPT_VALIDATION_FAILURE'
    write(out/'attempt.json',{'Command':command,'DeadlineSeconds':limit,'AuthorFreezeSha256':a.expected_freeze.upper(),'Mode':a.mode})
    try:
        proc=subprocess.Popen(command,cwd=C,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env)
        try:stdout,stderr=proc.communicate(timeout=limit);settled=True
        except subprocess.TimeoutExpired:
            expired=True
            try:
                killed=subprocess.run(['taskkill','/PID',str(proc.pid),'/T','/F'],capture_output=True,timeout=10);cleanup['TaskkillExitCode']=killed.returncode
            except subprocess.TimeoutExpired:cleanup['TaskkillTimedOut']=True
            try:stdout,stderr=proc.communicate(timeout=10);settled=proc.poll() is not None and cleanup.get('TaskkillExitCode')==0
            except subprocess.TimeoutExpired as error:
                stdout=error.stdout or b'';stderr=error.stderr or b'';cleanup['FinalPipeDrainTimedOut']=True;proc.kill()
                try:proc.wait(timeout=2)
                except subprocess.TimeoutExpired:cleanup['FinalRootWaitTimedOut']=True
        assert not expired and settled,'Owned process deadline/settlement failure'
        classification=validator(proc.returncode,out)
    except Exception:failure=traceback.format_exc()
    finally:
        (out/'stdout.log').write_bytes(stdout);(out/'stderr.log').write_bytes(stderr)
        try:sourcecheck();verify_refs()
        except Exception:
            failure=(failure or '')+traceback.format_exc();classification='SOURCE_OR_REFERENCE_VALIDATION_FAILURE'
        # Receipt failures are still evidence: always write envelope before caller raises.
        write(out/'process.json',{'Classification':classification,'Command':command,'ExitCode':proc.returncode if proc else None,'OwnedPid':proc.pid if proc else None,'TimedOut':expired,'OwnedProcessSettled':settled,'DeadlineSeconds':limit,'CleanupMaximumSeconds':22,'Cleanup':cleanup,'ElapsedSeconds':time.monotonic()-start,'ValidationError':failure,'AuthorFreezeSha256':a.expected_freeze.upper(),'RawReportSha256':sha(out/'actual.json') if (out/'actual.json').exists() else None})
    if cleanup.get('FinalPipeDrainTimedOut'):os._exit(1)
    return classification,failure
unowned=[e for e in rows if e['Path'] not in owned]
def zero(code,out):
    assert code==0;return 'PROCESS_EXIT_ZERO_NOT_FEATURE_COMPLETION'
def checked(label,command):
    result,error=run(label,command,120,zero,lambda:exact(C,unowned));assert not error,(label,error)
checked('token-before',['dotnet',str(token),str(D/'token-inputs.json'),str(D/'tokens-before.json')])
checked('sdk-format',['dotnet','format','whitespace','--folder',str(C),'--include',*inputs,'--verbosity','minimal'])
for n in owned:
    q=M/n;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes((C/n).read_bytes())
checked('style-write',['dotnet',str(style),'--write',str(M)])
for n in owned:(C/n).write_bytes((M/n).read_bytes())
checked('token-after',['dotnet',str(token),str(D/'token-inputs.json'),str(D/'tokens-after.json')])
checked('style-check',['dotnet',str(style),'--check',str(M)])
assert (D/'tokens-before.json').read_bytes()==(D/'tokens-after.json').read_bytes()
actual=[{'Path':e['Path'],'Bytes':(C/e['Path']).stat().st_size,'Sha256':sha(C/e['Path'])} for e in rows];exact(C,unowned)
write(D/'formatted-source-resource-closure.json',{'Files':actual})
write(D/'formatted-source-freeze.json',{'Status':'FORMATTED_PENDING_ACTUAL_TARGET_SELECTORS_AND_ORDINARY','AuthorFreezeSha256':a.expected_freeze.upper(),'SourcePaths':len(owned),'CSharpPaths':len(owned),'SourceClosurePaths':len(rows),'SourceClosureSha256':sha(D/'formatted-source-resource-closure.json'),'Files':[e for e in actual if e['Path'] in owned],'Selectors':selectors,'AllOwnedTokensByteExact':True,'OutsideOwnedRawExact':len(unowned),'WhitespaceChangedPaths':[e['Path'] for e,b in zip(actual,rows) if e!=b],'NativePlansNotResealed':True})
settings=['--disable-build-servers','-m:1','-nr:false','-p:UseSharedCompilation=false','-p:RestoreDisableParallel=true','--nologo']
result,error=run('target-build',['dotnet','build',str(C/'tests/Erlang.Tests/Erlang.Tests.csproj'),*settings],180,zero,lambda:exact(C,actual));assert not error,error
tests=C/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll';binarypins={name:sha(tests.with_name(name+'.dll')) for name in ['Erlang.Tests','Erlang.Runtime','Erlang.Compiler']}
results=[]
for index,selector in enumerate(selectors):
    label=f'selector-{index:02d}';report=D/label/'actual.json'
    def validate(code,out):
        v=read(out/'actual.json');assert len(v['Cases'])==1 and v['Cases'][0]['Name']==selector
        names={x['Name'] for x in v['Identities']};assert len(names)==len(v['Identities']) and {'Erlang.Tests','Erlang.Runtime'}<=names
        for x in v['Identities']:
            q=Path(x['Path']);assert x['Name'] in binarypins and q.resolve()==tests.with_name(x['Name']+'.dll').resolve() and sha(q)==x['Sha256']==binarypins[x['Name']]
        case=v['Cases'][0]
        if code==0 and case['Status']=='PASS':return 'SELECTOR_PASS'
        error=case.get('Exception','')
        # Every failure is preserved as actual diagnostic evidence; none is accepted as historical RED.
        return 'ACTUAL_SELECTOR_FAILURE_NOT_EXPECTED_SEMANTIC_RED'
    def source_and_binaries():
        exact(C,actual)
        for name,digest in binarypins.items():assert sha(tests.with_name(name+'.dll'))==digest
    result,error=run(label,['dotnet',str(focus),str(tests),str(report),selector],120,validate,source_and_binaries)
    results.append({'Selector':selector,'Classification':result,'ValidationError':error})
passed=not any(x['ValidationError'] for x in results) and all(x['Classification']=='SELECTOR_PASS' for x in results)
write(D/'diagnostic-summary.json',{'Status':'EXPECTED_DIAGNOSTIC_CONTRACT_SATISFIED' if passed else 'DIAGNOSTIC_HAS_FAILURES_OR_EXPECTATION_MISMATCH','Passed':passed,'Mode':a.mode,'AuthorFreezeSha256':a.expected_freeze.upper(),'FormattedFreezeSha256':sha(D/'formatted-source-freeze.json'),'SourceFiles':len(rows),'OwnedCSharpFiles':len(owned),'TargetBuildExitZero':True,'Results':results,'OrdinaryCasesNotExecuted':True,'ProductionCompletionNotClaimed':True,'MainOrOriginalOrNativeExecution':False})
print(str(D/'diagnostic-summary.json'));raise SystemExit(0 if passed else 1)
