"""Passive354 full reader. Execution requires Root terminal notice and source approval."""
from pathlib import Path,PurePosixPath
import collections,hashlib,json,re,zipfile,xml.etree.ElementTree as ET
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
A=R/'artifacts/size-bif354-full-actual2';C=R/'artifacts/size-bif354-full-candidate2'
P=R/'artifacts/size-bif354-full2-preparation/composition-plan.json'
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def hb(b):return hashlib.sha256(b).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
assert h(P)=='2E0A920C615F96CE8FE0F4BA91DFE0EDB6DF4853ABB71801E78FF67E8C602A2B'
sourcefreeze=R/'artifacts/size-bif354-full2-preparation/source-freeze.json';assert h(sourcefreeze)=='9430690ABC71D6500493840B63213851D0A921A0B57ABFB124295DEB3CBF9EC4'
for row in rd(sourcefreeze)['Files']:
 q=sourcefreeze.parent/row['Path'];assert q.stat().st_size==row['Bytes'] and h(q)==row['Sha256'],row['Path']
assert h(R/'artifacts/size-bif354-full2-preparation/run-full.py')=='6DD4D16F6DAD582FE90E1975FFC3C6E5070D246AD9C7B6529F720D3D26668C56'
assert h(R/'artifacts/spawn-priority351-preparation/execute-root.py')=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
plan=rd(P);assert len(plan['Files'])==3209 and len(plan['StableClosure'])==3208 and len(plan['OwnedPaths'])==131
assert (A/'composition-plan.json').read_bytes()==P.read_bytes()
inputs=rd(A/'inputs.json');assert inputs['PlanSha256']==h(P) and inputs['RunnerSha256']==h(R/'artifacts/size-bif354-full2-preparation/run-full.py')
assert inputs['OwnershipHelperSha256']==h(R/'artifacts/spawn-priority351-preparation/execute-root.py')
assert Path(inputs['Candidate']).resolve()==C.resolve() and inputs['RegistryWillNotBeRestored'] and not inputs['OriginalOtpExecuted']
assert h(A/'runner-source-review.json')==inputs['SourcePeerSha256'] and rd(A/'runner-source-review.json')['Status'].startswith('PASS')

focus_guard=A/'corrected-focus-independent.json'
assert h(focus_guard)==inputs['AcceptedFocusGuardSha256']=='AAB2EF6AC9EFA11D0A8F7E179E539B1548E91E3F4ADACB20681F574AF0A2FD33'
fg=rd(focus_guard);assert fg['Status']=='PASS_INDEPENDENT354_DIAGNOSTIC_ACQUISITION_WITH_SEPARATE_SEMANTIC_OUTCOMES' and fg['SemanticPassed']==44 and fg['SemanticFailed']==0 and fg['ManagedExecuted']==44 and fg['ParseAll']==36
assert fg['SourceFreezeSha256']=='F804F2FF04F8ACCA2465270DA0155448B7DA6FD1A3ED6C6C9708C7FA2AF71B1A'
ff=R/'artifacts/size-bif354-focus-execution-proposal14/source-freeze.json';assert h(ff)==fg['SourceFreezeSha256']
fi=R/'artifacts/size-bif354-focus-execution-proposal14/execution-inputs.json';assert h(fi)==next(r['Sha256'] for r in rd(ff)['Files'] if r['Path']=='execution-inputs.json')
focus_rows={r['Path']:r for r in rd(fi)['Rows']}
code_rows=[r for r in plan['Files'] if r['Path'].startswith(('src/','tests/Erlang.Tests/','tools/Erlang.Differential/'))]
assert len(code_rows)==inputs['FocusedProductionAndTestInputRowsIdentical']==2210
for r in code_rows:assert (r['Bytes'],r['Sha256'])==(focus_rows[r['Path']]['Bytes'],focus_rows[r['Path']]['Sha256'])
historical=R/'artifacts/root-size354-initial2-independent.json';assert h(historical)=='7B1B4599B536AC9E6B83888FD10A7D64A48EC2E583250A627D16AD155EC4EAD9'
assert (rd(historical)['SemanticPassed'],rd(historical)['SemanticFailed'])==(15,2)
for row in plan['InputPins']:
 q=R/row['Path'];assert q.stat().st_size==row['Bytes'] and h(q)==row['Sha256'],row['Path']
for row in plan['StableClosure']:
 p=C/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256'],row['Path']
assert rd(A/'stable-closure-after.json')==dict(VerifiedRows=3208,PlanSha256=h(P),Unchanged=True)
assert h(A/'registry-before.json')==plan['RegistryBefore']['Sha256']
process=rd(A/'full.live.json');exit=rd(A/'full.exit.json')
assert exit['ExitCode']==1 and exit['Settled'] and not exit['TimedOut'] and exit['Failures']==[]
assert exit['Pid']==process['Pid'] and isinstance(exit['Pid'],int) and exit['Pid']>0 and exit['Command']==process['Command']
assert 0<exit['ElapsedSeconds']<930
assert exit['StdoutSha256']==h(A/'full.stdout.log') and exit['StderrSha256']==h(A/'full.stderr.log')
assert exit['Command'][-1]==str(C/'tools/validate.ps1')
assert not (A/'producer-completion.json').exists()
captured=rd(A/'captured-outputs.json');assert captured['RawRegistrySha256']==h(A/'raw-supported-mfas.json') and not captured['RegistryRestored']
for row in captured['Outputs']:
 p=A/row['Path'];assert p.read_bytes()==(C/row['Path']).read_bytes() and p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
required={'artifacts/tests.json'}
assert {x['Path'] for x in captured['Outputs']}==required and len(captured['Outputs'])==1
assert captured['Packages']==[] and not list((C/'artifacts/packages').glob('*.nupkg'))
assert not (A/'artifacts/integration-results.json').exists() and not (C/'artifacts/integration-results.json').exists()
tests=rd(A/'artifacts/tests.json');old=rd(A/'previous-tests.json')
assert (A/'previous-tests.json').read_bytes()==(C/plan['PreviousTestReport']).read_bytes()
previous=[x['Name'] for x in old['Tests']];names=[x['Name'] for x in tests['Tests']]
assert old['Passed']==len(previous)==len(set(previous))==7313 and old['Failed']==0 and all(x['Status']=='Passed' for x in old['Tests'])
assert len(names)==len(set(names))==7342 and tests['Passed']==7341 and tests['Failed']==1
assert sum(x['Status']=='Passed' for x in tests['Tests'])==7341 and sum(x['Status']=='Failed' for x in tests['Tests'])==1
observed_file=O/'observed-failures.json';observed=rd(observed_file)
assert observed['TestReportSha256']==h(A/'artifacts/tests.json') and observed['FullExitSha256']==h(A/'full.exit.json')
failed=[x for x in tests['Tests'] if x['Status']=='Failed'];assert failed==observed['FailureRows']
assert failed[0]['Name']=='runtime/halt-live-port-drain-immediate-timeout-and-profile-ownership'
assert failed[0]['Error'].startswith('System.TimeoutException:') and failed[0]['Milliseconds']==15014
priorfail=R/'artifacts/size-bif354-failed-full-independent/actual-report.json';assert h(priorfail)=='1AF416F070A2D8996430DA580CE3ABD3F48DD864C7D88B76EB849728F342F0EB'
oldfail=rd(priorfail)['FailureRows'];assert len(oldfail)==18
byname={x['Name']:x for x in tests['Tests']}
assert all(byname[x['Name']]['Status']=='Passed' for x in oldfail)
assert names[:7313]==previous
added=plan['AddedTestNames'];assert len(added)==29 and names[7313:]==added
ledger=rd(C/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==115
counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']));assert counts=={'Verified compatible':16,'Known differences':30,'Needs verification':541}
for row in ledger['VerifiedSourceFiles']:assert row['Path'].startswith('src/') and h(C/row['Path'])==row['Sha256']
evidence={}
for entry in ledger['Entries']:
 for row in entry['Evidence']:assert h(C/'docs'/row['Path'])==row['Sha256'];evidence[row['Path']]=row['Sha256']
raw=rd(A/'raw-supported-mfas.json');baseline=rd(A/'registry-before.json')
assert len(raw)==len(baseline)==587 and len({(x['Module'],x['Function'],x['Arity']) for x in raw})==587
for actual,before in zip(raw,baseline,strict=True):assert {k:v for k,v in actual.items() if k!='Limits'}=={k:v for k,v in before.items() if k!='Limits'}
assert all(isinstance(x['Limits'],str) for x in raw+baseline)
assert h(C/'docs/supported-mfas.json')==h(A/'raw-supported-mfas.json')
assert sum(x['Module']=='proc_lib' for x in raw)==39 and len({x['Module'] for x in raw})==14
child_root=C/'artifacts/term-order-children';fresh={p.name for p in child_root.iterdir() if p.is_dir()}
assert len(captured['Children'])==12 and {x['Path'] for x in captured['Children']}=={p.relative_to(C).as_posix() for p in child_root.glob('*/receipt.json')}
child_manifest={'Files':[]}
for row in captured['Children']:
 assert h(C/row['Path'])==row['Sha256']
 child_manifest['Files'].append(dict(Path=row['Path'],ReceiptSha256=row['Sha256'],Value=rd(C/row['Path'])))
assert {Path(x['Path']).parent.name for x in child_manifest['Files']}==fresh and len(fresh)==12
matrix={(family,op,32768) for family in ['tuple','list-head','map-value','fun-environment','native-record'] for op in ['compare','hash']}|{('tuple','shallow',6),('tuple','snapshot',0)}
observed=[];pids=[];terms=C/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Terms.dll';driver=C/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll'
for row in child_manifest['Files']:
 path=C/row['Path'];v=rd(path);assert h(path)==row['ReceiptSha256'] and v==row['Value']
 assert v['Started'] and v['Settled'] and not v['TimedOut'] and not v['KillRequested'] and v['ExitCode']==0 and not v['LifecycleFailures'] and v['StandardOutputComplete'] and v['StandardErrorComplete']
 assert isinstance(v['ProcessId'],int) and v['ProcessId']>0 and 0<=v['ElapsedSeconds']<=60
 assert Path(v['DriverPath']).resolve()==driver.resolve();pids.append(v['ProcessId'])
 directory=path.parent;assert h(directory/'stdout.txt')==v['StandardOutputSha256'] and h(directory/'stderr.txt')==v['StandardErrorSha256'] and (directory/'stderr.txt').read_bytes()==b''
 lines=(directory/'stdout.txt').read_text(encoding='utf-8-sig').splitlines();loaded=json.loads(lines[0]);assert Path(loaded['TermsAssemblyPath']).resolve()==terms.resolve() and loaded['TermsAssemblySha256']==v['TermsDllSha256']==h(terms) and lines[-1]=='PASS'
 if v['Operation'] in ['compare','hash']:assert 'CONSTRUCTED' in lines
 observed.append((v['Family'],v['Operation'],v['Depth']))
assert len(observed)==len(set(observed))==12 and set(observed)==matrix


native_plans=[]
for path,wf,var,n in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1282),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1307)]:
 b=(C/path).read_bytes();assert b'\r' not in b and not b.startswith(b'\xef\xbb\xbf');manifest=json.loads(b);assert len(manifest['Files'])==n
 for row in manifest['Files']:
  data=(C/row['Path']).read_bytes();assert row['HashMode'] in ['TextLF','BinaryExact']
  if row['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
  assert len(data)==row['Bytes'] and hb(data)==row['Sha256'],row['Path']
 assert re.findall(var.encode()+rb': ([A-F0-9]{64})',(C/wf).read_bytes())==[h(C/path).encode()]
 native_plans.append(dict(Path=path,Rows=n,Sha256=h(C/path),AllRowsAndSealVerified=True))

assert len(captured['TestDlls'])==len({r['Path'] for r in captured['TestDlls']})
assert {r['Path'] for r in captured['TestDlls']}=={p.relative_to(C).as_posix() for p in (C/'tests/Erlang.Tests/bin/Debug/net10.0').glob('*.dll')}
for row in captured['TestDlls']:
 p=C/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
text=(A/'full.stdout.log').read_text(encoding='utf-8-sig')
for english,russian in ((r'^\s*([0-9]+) Warning\(s\)\s*$',r'^\s*Предупреждений:\s*([0-9]+)\s*$'),(r'^\s*([0-9]+) Error\(s\)\s*$',r'^\s*Ошибок:\s*([0-9]+)\s*$')):
 hits=re.findall(english,text,re.M)+re.findall(russian,text,re.M);assert hits and all(int(x)==0 for x in hits)
result=dict(Status='PASS_INDEPENDENT_ACTUAL_FAILED_CANONICAL354_FULL2_ACQUISITION_NO_FULL_ACCEPTANCE',Passed=7341,Failed=1,Tests=7342,FullAccepted=False,CompositionPlanSha256=h(P),StableClosureRows=3208,OwnedPaths=131,Old7313OrderedPrefix=True,AddedTestNames=added,All29NewPermanentTestsPassed=all(x['Status']=='Passed' for x in tests['Tests'][7313:]),FailureRows=failed,FailureScope='Canonical whole old HaltPort Run outer15s timeout only; no private phase/root cause proven.',AllPrior18FailuresNowPassed=True,PriorFailureNames=[x['Name'] for x in oldfail],PriorFailureAcquisitionGuardSha256=h(priorfail),IntegrationResultsProduced=False,PackagesProduced=0,RuntimeSourceGuards=115,CompatibilityCounts=counts,LedgerEvidenceRows=len(evidence),RegistryRows=587,RegistryNonLimitsAndOrderPreserved=True,RegistryBeforeSha256=h(A/'registry-before.json'),RawRegistrySha256=h(A/'raw-supported-mfas.json'),RegistryNotRestored=True,TermOrderChildren=12,ChildMatrixVerified=True,ActualChildTermsIdentity=h(terms),DriverIdentityScope='Recorded driver path/current hash; Terms identity emitted by child. No initial full DLL set claimed.',FullExitSha256=h(A/'full.exit.json'),FullStdoutSha256=h(A/'full.stdout.log'),FullStderrSha256=h(A/'full.stderr.log'),TestReportSha256=h(A/'artifacts/tests.json'),CapturedIndexSha256=h(A/'captured-outputs.json'),NativeSourcePlans=native_plans,FocusedProductionAndTestInputRowsIdentical=2210,AcceptedFocusGuardSha256=h(focus_guard),ReaderSha256=h(Path(__file__)),NoSdkOrOriginalOtpExecution=True,MainModified=False,NoCompatibilityPromotion=True,PassReaderPreservedUnexecuted='artifacts/size-bif354-full2-independent/review.py')
out=O/'actual-report.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(h(out))
