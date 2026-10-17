"""Passive354 full reader. Execution requires Root terminal notice and source approval."""
from pathlib import Path,PurePosixPath
import collections,hashlib,json,re,zipfile,xml.etree.ElementTree as ET
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
A=R/'artifacts/size-bif354-full-actual1';C=R/'artifacts/size-bif354-full-candidate'
P=R/'artifacts/size-bif354-full-preparation/composition-plan.json'
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def hb(b):return hashlib.sha256(b).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
assert h(P)=='7129614325BD320F9205DBFDEBC74E3C7F09065C318431360B64FA6A078890C6'
assert h(R/'artifacts/size-bif354-full-preparation/run-full2.py')=='5354F9424D23548984E45358F7117CBBD9D2D9706131C4C5AE3453BB1D3F5C60'
assert h(R/'artifacts/spawn-priority351-preparation/execute-root.py')=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
plan=rd(P);assert len(plan['Files'])==3208 and len(plan['StableClosure'])==3207 and len(plan['OwnedPaths'])==128
assert (A/'composition-plan.json').read_bytes()==P.read_bytes()
inputs=rd(A/'inputs.json');assert inputs['PlanSha256']==h(P) and inputs['RunnerSha256']==h(R/'artifacts/size-bif354-full-preparation/run-full2.py')
assert inputs['OwnershipHelperSha256']==h(R/'artifacts/spawn-priority351-preparation/execute-root.py')
assert Path(inputs['Candidate']).resolve()==C.resolve() and inputs['RegistryWillNotBeRestored'] and not inputs['OriginalOtpExecuted']
assert h(A/'runner-source-review.json')==inputs['SourcePeerSha256'] and rd(A/'runner-source-review.json')['Status'].startswith('PASS')

focus_guard=A/'corrected-focus-independent.json'
assert h(focus_guard)==inputs['AcceptedFocusGuardSha256']=='2DABAA680DF8CC8F7483B4437EA8385FC66D415B094B977BCC3EF1A68338D35D'
fg=rd(focus_guard);assert fg['Status']=='PASS_INDEPENDENT354_DIAGNOSTIC_ACQUISITION_WITH_SEPARATE_SEMANTIC_OUTCOMES' and fg['SemanticPassed']==36 and fg['SemanticFailed']==0 and fg['ManagedExecuted']==36 and fg['ParseAll']==28
assert fg['SourceFreezeSha256']=='AEE9A8BC6A40DD2F29DA835705E73215FBB500C2F1E97700F8CA55139FBD0D83'
ff=R/'artifacts/size-bif354-focus-execution-proposal11/source-freeze.json';assert h(ff)==fg['SourceFreezeSha256']
fi=R/'artifacts/size-bif354-focus-execution-proposal11/execution-inputs.json';assert h(fi)==next(r['Sha256'] for r in rd(ff)['Files'] if r['Path']=='execution-inputs.json')
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
assert rd(A/'stable-closure-after.json')==dict(VerifiedRows=3207,PlanSha256=h(P),Unchanged=True)
assert h(A/'registry-before.json')==plan['RegistryBefore']['Sha256']
process=rd(A/'full.live.json');exit=rd(A/'full.exit.json')
assert exit['ExitCode']==0 and exit['Settled'] and not exit['TimedOut'] and exit['Failures']==[]
assert exit['Pid']==process['Pid'] and isinstance(exit['Pid'],int) and exit['Pid']>0 and exit['Command']==process['Command']
assert 0<exit['ElapsedSeconds']<930
assert exit['StdoutSha256']==h(A/'full.stdout.log') and exit['StderrSha256']==h(A/'full.stderr.log')
assert exit['Command'][-1]==str(C/'tools/validate.ps1')
producer=rd(A/'producer-completion.json')
assert producer==dict(Status='ACTUAL_CANONICAL_FULL354_COMMAND_EXITED_ZERO_REQUIRES_INDEPENDENT_REVIEW',IndependentAcceptance=False,OriginalOtpExecuted=False,MainChanged=False)
captured=rd(A/'captured-outputs.json');assert captured['RawRegistrySha256']==h(A/'raw-supported-mfas.json') and not captured['RegistryRestored']
for row in captured['Outputs']:
 p=A/row['Path'];assert p.read_bytes()==(C/row['Path']).read_bytes() and p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
required={'artifacts/tests.json','artifacts/integration-results.json','artifacts/readiness/module-readiness.json','artifacts/readiness/MODULE_READINESS.md','artifacts/readiness/API_COMPATIBILITY.md','artifacts/tool-readiness/module-readiness.json'}
assert {x['Path'] for x in captured['Outputs']}==required and len(captured['Outputs'])==6
tests=rd(A/'artifacts/tests.json');old=rd(A/'previous-tests.json')
assert (A/'previous-tests.json').read_bytes()==(C/plan['PreviousTestReport']).read_bytes()
previous=[x['Name'] for x in old['Tests']];names=[x['Name'] for x in tests['Tests']]
assert old['Passed']==len(previous)==len(set(previous))==7313 and old['Failed']==0 and all(x['Status']=='Passed' for x in old['Tests'])
assert tests['Passed']==len(names)==len(set(names))==7342 and tests['Failed']==0 and all(x['Status']=='Passed' for x in tests['Tests'])
assert names[:7313]==previous
added=plan['AddedTestNames'];assert len(added)==29 and names[7313:]==added
integrations=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed')
assert rd(A/'artifacts/integration-results.json')==integrations
ledger=rd(C/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==114
counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']));assert counts=={'Verified compatible':16,'Known differences':30,'Needs verification':541}
for row in ledger['VerifiedSourceFiles']:assert row['Path'].startswith('src/') and h(C/row['Path'])==row['Sha256']
evidence={}
for entry in ledger['Entries']:
 for row in entry['Evidence']:assert h(C/'docs'/row['Path'])==row['Sha256'];evidence[row['Path']]=row['Sha256']
readiness=[]
for path in ['artifacts/readiness/module-readiness.json','artifacts/tool-readiness/module-readiness.json']:
 v=rd(A/path);assert v['RegisteredMfas']==587 and v['VerifiedCompatibleMfas']==16 and v['KnownDifferenceMfas']==30 and v['NeedsVerificationMfas']==541
 assert v['VerifiedSourceFiles']==ledger['VerifiedSourceFiles'] and v['LocalPassedTests']==7342 and v['ImplementedApiModules']==14
 assert v['ComparisonTestReport']=='validation/part-353/full/raw/artifacts/tests.json'
 readiness.append(dict(Path=path,Sha256=h(A/path)))
raw=rd(A/'raw-supported-mfas.json');baseline=rd(A/'registry-before.json')
assert len(raw)==len(baseline)==587 and len({(x['Module'],x['Function'],x['Arity']) for x in raw})==587
for actual,before in zip(raw,baseline,strict=True):assert {k:v for k,v in actual.items() if k!='Limits'}=={k:v for k,v in before.items() if k!='Limits'}
assert all(isinstance(x['Limits'],str) for x in raw+baseline)
assert h(C/'docs/supported-mfas.json')==h(A/'raw-supported-mfas.json')
assert sum(x['Module']=='proc_lib' for x in raw)==39 and len({x['Module'] for x in raw})==14
guard={'PackageOutputs':2,'BoundedChildControls':12}
package_manifest=captured['Packages'];assert len(package_manifest)==2
assert {x['Path'] for x in package_manifest}=={p.relative_to(C).as_posix() for p in (C/'artifacts/packages').glob('*.nupkg')}
package_inputs={Path(x['Path']).name:C/x['Path'] for x in plan['PackageResources']};assert len(package_inputs)==6
assert h(package_inputs['OTP-PORTS.md'])=='8CDFA5AF3CE497F52A1B2DF9ACAF1B40DCC2B3F5B3ECA454F70D7AEC9C127CE8'
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
result=dict(Status='PASS_INDEPENDENT_ACTUAL_CANONICAL354_FULL_RAW_REGISTRY_RESTORATION_PENDING',Passed=7342,Failed=0,CompositionPlanSha256=h(P),SourceFreezeSha256=h(R/'artifacts/size-bif354-full-preparation/source-freeze2.json'),SourceClosureSha256=hb(json.dumps(plan['StableClosure'],sort_keys=True,separators=(',',':')).encode()),StableClosureRows=3207,OwnedPaths=128,Old7313ExactPrefix=True,AddedTestNames=added,IntegrationResults=integrations,Packages=package_results,PackageInputs=6,RuntimeSourceGuards=114,CompatibilityCounts=counts,LedgerEvidenceRows=len(evidence),Readiness=readiness,RegistryRows=587,RegistryNonLimitsAndOrderPreserved=True,RegistryBeforeSha256=h(A/'registry-before.json'),RawRegistrySha256=h(A/'raw-supported-mfas.json'),CustomLimitsRestorationPending=True,TermOrderChildren=12,ChildMatrixVerified=True,ActualChildTermsIdentity=h(terms),DriverIdentityScope='Recorded driver path/current hash; only Terms identity is independently emitted by child. No initial DLL set claimed.',ProducerFreshChildrenScope='Reviewed wrapper refused prior receipts before launch; exact12 current directories checked. No invented children-before snapshot.',FullExitSha256=h(A/'full.exit.json'),FullStdoutSha256=h(A/'full.stdout.log'),FullStderrSha256=h(A/'full.stderr.log'),TestReportSha256=h(A/'artifacts/tests.json'),CapturedIndexSha256=h(A/'captured-outputs.json'),NativeSourcePlans=native_plans,FocusedProductionAndTestInputRowsIdentical=2210,AcceptedCorrectedFocusGuardSha256=h(focus_guard),HistoricalGeneric15Pass2FailGuardSha256=h(historical),SingleUninterruptedPassScope='Prospective full354 canonical attempt only. Focus acquisition/initial generic and handler failures remain retained as history.',ReaderSha256=h(Path(__file__)),NativeOriginalExecuted=False,MainModified=False,NoCompatibilityPromotionInferredFromFull=True)
out=O/'full-report.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(h(out))
