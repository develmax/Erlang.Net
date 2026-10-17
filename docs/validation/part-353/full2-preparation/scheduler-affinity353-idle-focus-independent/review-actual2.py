"""Passive corrected focus40 reader; execute only after Root source approval."""
import hashlib,json,re
from pathlib import Path
R=Path(__file__).resolve().parents[2];O=Path(__file__).parent
A=R/'artifacts/scheduler-affinity353-idle-focus-actual2';P=R/'artifacts/scheduler-affinity353-idle-focus2-source'
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
assert h(P/'extended-source-freeze.json')=='513564D6C8BEDB585CF938EA85BD83D08296B0F4B24122635D4F170B9A9DBA6D'
assert h(R/'artifacts/root-affinity353-idle-focus2.py')=='95D9CA7A8FB57B0ACACFA862A21E1AB5D8D0BA2E9F38D40D7F547C120C6F28AE'
assert h(R/'artifacts/spawn-priority351-preparation/execute-root.py')=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
freeze=rd(P/'extended-source-freeze.json');C=Path(freeze['Candidate']);assert len(freeze['Rows'])==3120
for row in freeze['Rows']:
 p=C/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
inputs=rd(A/'inputs.json');assert inputs['FocusFreezeSha256']==h(P/'extended-source-freeze.json') and inputs['RunnerSha256']==h(R/'artifacts/root-affinity353-idle-focus2.py')
assert inputs['HelperSha256']==h(R/'artifacts/spawn-priority351-preparation/execute-root.py') and Path(inputs['Candidate']).resolve()==C.resolve()
assert inputs['CanonicalBudgetsUnchanged'] and inputs['MetadataIntentionallyStale'] and inputs['FullOrReadinessExcluded']
targets=rd(R/'artifacts/scheduler-affinity352-proposal6/focus-targets.json')['Targets'];assert len(targets)==len(set(targets))==30 and inputs['Targets30']==targets
old_diag=R/'artifacts/scheduler-affinity353-failure-diagnostic-proposal/source-freeze.json';assert h(old_diag)=='4337BE75AC7543086807B883D80687B34E6B07A579AD9840354C56EC89BF8102'
phases=rd(old_diag)['Targets'];assert len(phases)==len(set(phases))==10 and inputs['OriginalPhases10']==phases
diag=R/'artifacts/scheduler-affinity353-failure-diagnostic-actual1/build/Scheduler353DispatchDiagnostic.dll';assert inputs['DiagnosticDriverSha256']==h(diag)
for row in rd(diag.parent.parent/'diagnostic-dlls.json')['Files']:assert h(Path(row['Path']))==row['Sha256']
authority_path=R/'artifacts/scheduler-affinity352-focused-independent/source-contracts.json';assert h(authority_path)=='B949BAC0DBD4EAC4189B616654AA1012169409FBB9D5E9A269860EAA4B535C6A'
authority=rd(authority_path);contracts=authority['Contracts'];assert len(contracts)==7 and h(R/authority['Original350ReportPath'])==authority['Original350ReportSha256']
for row in authority['OldFixtureCatalogSourceBindingsRawExact350To351']:assert h(C/row['Path'])==row['Sha256']
expected_sources=[{k:r[k] for k in ['Name','SourceSha256','Expected']} for r in contracts];contractmap={r['Name']:r for r in contracts}
receipts=[]
def receipt(directory,name,limit):
 v=rd(directory/(name+'.exit.json'));live=rd(directory/(name+'.live.json'))
 assert v['Pid']==live['Pid'] and v['Command']==live['Command'] and isinstance(v['Pid'],int) and v['Pid']>0 and live['StartedUnixSeconds']>0
 assert v['ExitCode']==0 and v['Settled'] and not v['TimedOut'] and v['Failures']==[] and 0<v['ElapsedSeconds']<limit+20
 assert h(directory/(name+'.stdout.log'))==v['StdoutSha256'] and h(directory/(name+'.stderr.log'))==v['StderrSha256'] and (directory/(name+'.stderr.log')).read_bytes()==b''
 receipts.append(dict(Path=(directory/(name+'.exit.json')).relative_to(R).as_posix(),Sha256=h(directory/(name+'.exit.json')),Pid=v['Pid'],StartedUnixSeconds=live['StartedUnixSeconds']))
 return v
build=receipt(A,'build',240);buildtext=(A/'build.stdout.log').read_text(encoding='utf-8-sig')
for patterns in [[r'^\s*(\d+) Warning\(s\)\s*$',r'^\s*Предупреждений:\s*(\d+)\s*$'],[r'^\s*(\d+) Error\(s\)\s*$',r'^\s*Ошибок:\s*(\d+)\s*$']]:
 values=[int(v) for p in patterns for v in re.findall(p,buildtext,re.M)];assert values and all(v==0 for v in values)
initial=rd(A/'initial-built-dlls.json');focusmap=initial['FocusDlls'];testmap=initial['TestDlls'];assert len(focusmap)>=8 and len(testmap)>=8
focusdir=C/'tests/Erlang.Scheduler352.Focus/bin/Debug/net10.0';testdir=C/'tests/Erlang.Tests/bin/Debug/net10.0';tests=testdir/'Erlang.Tests.dll'
for directory,mapping in [(focusdir,focusmap),(testdir,testmap)]:
 assert set(mapping)=={str(p.resolve()) for p in directory.glob('*.dll')}
 for path,digest in mapping.items():assert h(Path(path))==digest
receipt(A,'parse',60);parsed=rd(A/'parse.json')
assert parsed['Name']=='--parse' and parsed['Passed'] and parsed['Failure'] is None and (A/'parse.stdout.log').read_text().strip()=='PARSE_ALL 7'
expected_loaded={'Erlang.Runtime.dll','Erlang.Terms.dll','Erlang.Serialization.dll','Erlang.Otp.Behaviours.dll','Erlang.Compiler.dll','Erlang.Differential.dll','Erlang.Tests.dll','Scheduler352Focus.dll'}
loaded_baseline=None
def focus_observation(v):
 global loaded_baseline
 assert v['Sources']==expected_sources and len(v['Targets'])==len(set(v['Targets']))==30 and set(v['Targets'])==set(targets)
 assert not v['NativeExecuted'] and not v['FullValidationExecuted'] and not v['StatusesPromoted']
 rows=v['LoadedAssemblies'];assert len(rows)==len({r['Path'] for r in rows})==8 and {Path(r['Path']).name for r in rows}==expected_loaded
 actual={str(Path(r['Path']).resolve()):r['Sha256'] for r in rows}
 for path,digest in actual.items():assert focusmap[path]==digest==h(Path(path))
 if loaded_baseline is None:loaded_baseline=actual
 assert actual==loaded_baseline
focus_observation(parsed);target_results=[]
for index,name in enumerate(targets):
 d=A/('target-'+str(index).zfill(2));receipt(d,'process',90);v=rd(d/'outcome.json')
 assert v['Name']==name and v['Passed'] and v['Failure'] is None and (d/'process.stdout.log').read_text().strip()=='PASS '+name
 focus_observation(v)
 if name in contractmap:
  obs=v['Observation'];assert obs['Name']==name and obs['Expected']==obs['Actual']==contractmap[name]['Expected'] and re.fullmatch('[0-9A-F]{64}',obs['CSharpSha256']) and 'Passed' not in obs
 else:assert v['Observation'] is None
 target_results.append(dict(Name=name,OutcomeSha256=h(d/'outcome.json')))
phase_results=[]
for index,name in enumerate(phases):
 d=A/('phase-'+str(index).zfill(2));life=receipt(d,'process',20 if name=='Run' else 45);v=rd(d/'outcome.json')
 assert life['Command']==['dotnet',str(diag),'--assembly',str(tests),'--target',name,'--output',str(d/'outcome.json')]
 assert v['Target']==name and v['Passed'] and v['Failure'] is None and v['InputAssembliesUnchanged'] and not v['OriginalMethodBodiesModified']
 assert v['OriginalWholeTestBudgetSeconds']==15 and not v['OriginalWholeTestOuterBudgetExceeded'] and 0<=v['ElapsedSeconds']<15
 assert not v['NativeExecuted'] and not v['FullValidationExecuted']
 assert {str(Path(r['Path']).resolve()):r['Sha256'] for r in v['BeforeAssemblies']}==testmap
 loaded=v['LoadedAssemblies'];assert len(loaded)==len({r['Path'] for r in loaded}) and any(Path(r['Path']).resolve()==tests.resolve() for r in loaded)
 for row in loaded:assert testmap[str(Path(row['Path']).resolve())]==row['Sha256']==h(Path(row['Path']))
 lines=(d/'process.stdout.log').read_text(encoding='utf-8-sig').splitlines();first=json.loads(lines[0]);last=json.loads(lines[-1])
 assert first['Phase']=='START' and first['Target']==name and Path(first['Assembly']).resolve()==tests.resolve() and first['Sha256']==h(tests)
 assert last['Phase']=='PASS' and last['Target']==name and last['Failure'] is None
 phase_results.append(dict(Name=name,ElapsedSeconds=v['ElapsedSeconds'],OutcomeSha256=h(d/'outcome.json')))
completion=rd(A/'completion.json');assert completion['Status']=='ACTUAL_CORRECTED_IDLE_STEAL_FOCUS40_REQUIRES_INDEPENDENT_REVIEW'
assert completion['ManagedPassed']==40 and completion['ParseAll']==7 and completion['Targets30']==target_results and completion['OriginalPhases10']==phase_results
assert completion['SourceFreezeSha256']==h(P/'extended-source-freeze.json') and completion['ClosureRows']==3120
assert all(not completion[k] for k in ['FullValidationExecuted','ReadinessExecuted','NativeExecuted','StatusesPromoted','MainChanged'])
assert len(receipts)==42
for mapping in [focusmap,testmap]:
 for path,digest in mapping.items():assert h(Path(path))==digest
old=R/'artifacts/scheduler-affinity353-failure-diagnostic-independent/actual-report.json';assert h(old)=='D2F1D000811BC383110A60E87F9A3987B54D678CA420E2657B027E32F40BD0A4'
assert rd(old)['DiagnosticPassed']==7 and rd(old)['DiagnosticFailed']==3
result=dict(Status='PASS_INDEPENDENT_ACTUAL_CORRECTED353_FOCUS40_NO_FULL_ACCEPTANCE',ManagedPassed=40,ParseAll=7,SourceFreezeSha256=h(P/'extended-source-freeze.json'),ClosureRows=3120,RunnerSha256=h(R/'artifacts/root-affinity353-idle-focus2.py'),SourceContractsSha256=h(authority_path),Targets30=target_results,OriginalPhases10=phase_results,Receipts=receipts,SettledReceipts=42,InitialFocusDlls=len(focusmap),InitialTestDlls=len(testmap),ActualLoaded8UniqueAndInitialSetsVerified=True,AllDllsAndSourcesUnchanged=True,GeneratedHashScope='Actual64hex emitted hash checked, not regenerated by passive reader',OriginalFailedDiagnosticsPreservedSha256=h(old),OriginalCanonicalBudgetsUnchanged=True,RedGreenScope='Original firstWave failures bounded2/4 and Run contrasted with corrected actualsame testmethod assertions/deadlines; no historicalqueue trace or nativephysicalbalancer parity claim.',FullAccepted=False,ReadinessExecuted=False,NativeExecuted=False,CompatibilityStatusesPromoted=False,MainModified=False,SdkExecutedByReader=False,ReaderSha256=h(Path(__file__)))
out=O/'actual-report.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(h(out))
