"""Passive353 full reader. Execution requires Root terminal notice and source approval."""
from pathlib import Path,PurePosixPath
import collections,hashlib,json,re,zipfile,xml.etree.ElementTree as ET
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
A=R/'artifacts/scheduler-affinity353-full-actual2';C=R/'artifacts/scheduler-affinity353-full-candidate2'
P=R/'artifacts/scheduler-affinity353-composition-plan2/composition-plan.json'
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def hb(b):return hashlib.sha256(b).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
assert h(P)=='3F8C5F95912959D7BE22BDDA3E6F39B696AC644EA98E5461C6B67C64771F06BC'
assert h(R/'artifacts/root-affinity353-full2.py')=='CC57902DB918ECD79474696EDD91FDFDB5985AFB0762D513392A5A62B720CB63'
assert h(R/'artifacts/spawn-priority351-preparation/execute-root.py')=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
plan=rd(P);assert len(plan['Files'])==3107 and len(plan['StableClosure'])==3106 and len(plan['OwnedPaths'])==169
assert (A/'composition-plan.json').read_bytes()==P.read_bytes()
inputs=rd(A/'inputs.json');assert inputs['PlanSha256']==h(P) and inputs['RunnerSha256']==h(R/'artifacts/root-affinity353-full2.py')
assert inputs['OwnershipHelperSha256']==h(R/'artifacts/spawn-priority351-preparation/execute-root.py')
assert Path(inputs['Candidate']).resolve()==C.resolve() and inputs['RegistryWillNotBeRestored'] and not inputs['OriginalOtpExecuted']
assert h(A/'runner-source-review.json')==inputs['SourcePeerSha256'] and rd(A/'runner-source-review.json')['Status'].startswith('PASS')

focus_guard=A/'corrected-focus-independent.json'
assert h(focus_guard)==inputs['AcceptedFocusGuardSha256']=='EC95B4234F95F3436D860EEB30B17772532E8FEA554D9F9A8C5EB7CDCEF86DE5'
fg=rd(focus_guard);assert fg['Status']=='PASS_INDEPENDENT_ACTUAL_CORRECTED353_FOCUS40_NO_FULL_ACCEPTANCE' and fg['ManagedPassed']==40 and fg['ParseAll']==7
assert fg['SourceFreezeSha256']=='513564D6C8BEDB585CF938EA85BD83D08296B0F4B24122635D4F170B9A9DBA6D' and fg['ReaderSha256']=='58B7F2A9FDA04CCDB896D42CBB252FE7462B980A99211858696586A79F1009A7'
assert h(R/'artifacts/scheduler-affinity353-idle-focus2-source/extended-source-freeze.json')==fg['SourceFreezeSha256']
focus_rows={r['Path']:r for r in rd(R/'artifacts/scheduler-affinity353-idle-focus2-source/extended-source-freeze.json')['Rows']}
code_rows=[r for r in plan['Files'] if r['Path'].startswith(('src/','tests/Erlang.Tests/','tools/Erlang.Differential/'))]
assert len(code_rows)==inputs['FocusedProductionAndTestInputRowsIdentical']==2180
for r in code_rows:assert (r['Bytes'],r['Sha256'])==(focus_rows[r['Path']]['Bytes'],focus_rows[r['Path']]['Sha256'])
failed1=R/'artifacts/scheduler-affinity353-full-actual1'
assert h(failed1/'full.exit.json')=='99BA270A7478F198C3ED08FB697F2053C27D720768DDD5C3571D50ADD946D8B7' and rd(failed1/'full.exit.json')['ExitCode']==1
assert h(failed1/'artifacts/tests.json')=='C0BD06B3CE79D002418B9C83E5CBE8EB349D925E633A474A8192335AB9649168'
assert rd(failed1/'artifacts/tests.json')['Passed']==7312 and rd(failed1/'artifacts/tests.json')['Failed']==1
assert not (failed1/'producer-completion.json').exists()
diagnostic=R/'artifacts/scheduler-affinity353-failure-diagnostic-independent/actual-report.json'
assert h(diagnostic)=='D2F1D000811BC383110A60E87F9A3987B54D678CA420E2657B027E32F40BD0A4'
assert rd(diagnostic)['DiagnosticPassed']==7 and rd(diagnostic)['DiagnosticFailed']==3

for row in plan['StableClosure']:
 p=C/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256'],row['Path']
assert rd(A/'stable-closure-after.json')==dict(VerifiedRows=3106,PlanSha256=h(P),Unchanged=True)
assert h(A/'registry-before.json')==plan['RegistryBefore']['Sha256']
process=rd(A/'full.live.json');exit=rd(A/'full.exit.json')
assert exit['ExitCode']==0 and exit['Settled'] and not exit['TimedOut'] and exit['Failures']==[]
assert exit['Pid']==process['Pid'] and isinstance(exit['Pid'],int) and exit['Pid']>0 and exit['Command']==process['Command']
assert 0<exit['ElapsedSeconds']<930
assert exit['StdoutSha256']==h(A/'full.stdout.log') and exit['StderrSha256']==h(A/'full.stderr.log')
assert exit['Command'][-1]==str(C/'tools/validate.ps1')
producer=rd(A/'producer-completion.json')
assert producer==dict(Status='ACTUAL_CANONICAL_FULL353_COMMAND_EXITED_ZERO_REQUIRES_INDEPENDENT_REVIEW',IndependentAcceptance=False,OriginalOtpExecuted=False,MainChanged=False)
captured=rd(A/'captured-outputs.json');assert captured['RawRegistrySha256']==h(A/'raw-supported-mfas.json') and not captured['RegistryRestored']
for row in captured['Outputs']:
 p=A/row['Path'];assert p.read_bytes()==(C/row['Path']).read_bytes() and p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
required={'artifacts/tests.json','artifacts/integration-results.json','artifacts/readiness/module-readiness.json','artifacts/readiness/MODULE_READINESS.md','artifacts/readiness/API_COMPATIBILITY.md','artifacts/tool-readiness/module-readiness.json'}
assert {x['Path'] for x in captured['Outputs']}==required and len(captured['Outputs'])==6
tests=rd(A/'artifacts/tests.json');old=rd(A/'previous-tests.json')
assert (A/'previous-tests.json').read_bytes()==(C/'docs/validation/part-351/full/tests.json').read_bytes()
previous=[x['Name'] for x in old['Tests']];names=[x['Name'] for x in tests['Tests']]
assert old['Passed']==len(previous)==len(set(previous))==7297 and old['Failed']==0 and all(x['Status']=='Passed' for x in old['Tests'])
assert tests['Passed']==len(names)==len(set(names))==7313 and tests['Failed']==0 and all(x['Status']=='Passed' for x in tests['Tests'])
assert names[:7297]==previous
permanent=rd(R/'artifacts/scheduler-affinity353-permanent-proposal2/report.json')
added=['oracle/compiled-module/'+x['Name'] for x in permanent['CompiledCases']]+[x['Name'] for x in permanent['HostTargets']]
assert len(added)==16 and names[7297:]==added
integrations=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed')
assert rd(A/'artifacts/integration-results.json')==integrations
ledger=rd(C/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==89
counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']));assert counts=={'Verified compatible':16,'Known differences':24,'Needs verification':547}
for row in ledger['VerifiedSourceFiles']:assert row['Path'].startswith('src/') and h(C/row['Path'])==row['Sha256']
evidence={}
for entry in ledger['Entries']:
 for row in entry['Evidence']:assert h(C/'docs'/row['Path'])==row['Sha256'];evidence[row['Path']]=row['Sha256']
readiness=[]
for path in ['artifacts/readiness/module-readiness.json','artifacts/tool-readiness/module-readiness.json']:
 v=rd(A/path);assert v['RegisteredMfas']==587 and v['VerifiedCompatibleMfas']==16 and v['KnownDifferenceMfas']==24 and v['NeedsVerificationMfas']==547
 assert v['VerifiedSourceFiles']==ledger['VerifiedSourceFiles'] and v['LocalPassedTests']==7313 and v['ImplementedApiModules']==14
 assert v['ComparisonTestReport']=='validation/part-351/full/tests.json'
 readiness.append(dict(Path=path,Sha256=h(A/path)))
raw=rd(A/'raw-supported-mfas.json');baseline=rd(A/'registry-before.json')
assert len(raw)==len(baseline)==587 and len({(x['Module'],x['Function'],x['Arity']) for x in raw})==587
for actual,before in zip(raw,baseline,strict=True):assert {k:v for k,v in actual.items() if k!='Limits'}=={k:v for k,v in before.items() if k!='Limits'}
assert h(C/'docs/supported-mfas.json')==h(A/'raw-supported-mfas.json')
assert sum(x['Module']=='proc_lib' for x in raw)==39 and len({x['Module'] for x in raw})==14
guard={'PackageOutputs':2,'BoundedChildControls':12}
package_manifest=captured['Packages'];assert len(package_manifest)==2
assert {x['Path'] for x in package_manifest}=={p.relative_to(C).as_posix() for p in (C/'artifacts/packages').glob('*.nupkg')}
package_inputs={Path(x['Path']).name:C/x['Path'] for x in plan['PackageResources']};assert len(package_inputs)==6
assert h(package_inputs['OTP-PORTS.md'])=='C941D5FA411A189EF2586420C4C38B513DEE00A3CFA4127A4B5101A8699EC8D1'
package_results=[]
ids={'Erlang.Net.CSharp','Erlang.Net.Tool'}
for row in package_manifest:
 p=C/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256'];checked=[]
 with zipfile.ZipFile(p) as z:
  files=[x for x in z.namelist() if not x.endswith('/')];assert len(files)==len(set(files)) and z.testzip() is None
  assert all(not PurePosixPath(x).is_absolute() and '..' not in PurePosixPath(x).parts and '\\' not in x for x in files)
  nuspec=[x for x in files if x.endswith('.nuspec')];assert len(nuspec)==1
  root=ET.fromstring(z.read(nuspec[0]));values={node.tag.split('}')[-1]:node.text for node in root.iter() if node.tag.split('}')[-1] in ['id','version']}
  assert values['id'] in ids and values['version']=='0.1.0';ids.remove(values['id'])
  for name,path in package_inputs.items():
   member='README.md' if name=='README.md' else 'licenses/'+name
   assert member in files and z.read(member)==path.read_bytes();checked.append(member)
  if values['id']=='Erlang.Net.CSharp':
   for project in ['Erlang.Terms','Erlang.Runtime','Erlang.Compiler','Erlang.Otp.Behaviours','Erlang.Serialization']:
    member='lib/net10.0/'+project+'.dll';assert z.read(member)==(C/'src'/project/'bin/Debug/net10.0'/(project+'.dll')).read_bytes();checked.append(member)
  else:
   for project in ['Erlang.Tool','Erlang.Terms','Erlang.Runtime','Erlang.Compiler']:
    member='tools/net10.0/any/'+project+'.dll';assert z.read(member)==(C/'tools/Erlang.Tool/bin/Debug/net10.0'/(project+'.dll')).read_bytes();checked.append(member)
  package_results.append(dict(Path=row['Path'],Sha256=row['Sha256'],Id=values['id'],Version=values['version'],ZipMembers=len(files),CrcAndSafeUniquePathsVerified=True,SourceBoundMembers=checked))
assert not ids

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
for path,wf,var,n in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1268),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1293)]:
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
result=dict(Status='PASS_INDEPENDENT_ACTUAL_CORRECTED_CANONICAL353_FULL2_RAW_REGISTRY_RESTORATION_PENDING',Passed=7313,Failed=0,CompositionPlanSha256=h(P),StableClosureRows=3106,OwnedPaths=169,Old7297ExactPrefix=True,AddedTestNames=added,IntegrationResults=integrations,Packages=package_results,PackageInputs=6,RuntimeSourceGuards=89,CompatibilityCounts=counts,LedgerEvidenceRows=len(evidence),Readiness=readiness,RegistryRows=587,RegistryNonLimitsAndOrderPreserved=True,RegistryBeforeSha256=h(A/'registry-before.json'),RawRegistrySha256=h(A/'raw-supported-mfas.json'),CustomLimitsRestorationPending=True,TermOrderChildren=12,ChildMatrixVerified=True,ActualChildTermsIdentity=h(terms),DriverIdentityScope='Recorded driver path/current hash; only Terms identity is independently emitted by child. No initial DLL set claimed.',ProducerFreshChildrenScope='Reviewed executed wrapper refused any prior receipt before launch; current exact12 indexed directories checked. No invented children-before raw snapshot.',FullExitSha256=h(A/'full.exit.json'),FullStdoutSha256=h(A/'full.stdout.log'),FullStderrSha256=h(A/'full.stderr.log'),TestReportSha256=h(A/'artifacts/tests.json'),CapturedIndexSha256=h(A/'captured-outputs.json'),NativeSourcePlans=native_plans,FocusedProductionAndTestInputRowsIdentical=2180,AcceptedCorrectedFocusGuardSha256=h(focus_guard),FailedFull1ExitSha256=h(failed1/'full.exit.json'),FailedFull1TestReportSha256=h(failed1/'artifacts/tests.json'),OriginalDiagnosticFailureEvidenceSha256=h(diagnostic),SingleUninterruptedPassScope='Corrected attempt2 only; actual1 failed7312/1 and retained. Exact old7297 testnames/method assertions andcanonical15 unchanged.',ReaderSha256=h(Path(__file__)),NativeOriginalExecuted=False,MainModified=False,LaterReportingOnlyDocuments='Current0e739 three reporting documents are raw-bound in composition2; promotion must preserve them or use separately reviewed appendix-only update.',NoCompatibilityPromotionInferredFromFull=True)
out=O/'full2-report.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(h(out))
