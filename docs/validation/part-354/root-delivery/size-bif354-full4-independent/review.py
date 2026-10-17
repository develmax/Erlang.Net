"""Passive full4 PASS/FAIL acquisition; wait Root terminal notice before execution."""
from pathlib import Path,PurePosixPath
import hashlib,json,re,zipfile,collections,xml.etree.ElementTree as ET
R=Path(__file__).resolve().parents[2];O=Path(__file__).parent
A=R/'artifacts/size-bif354-full-actual4';C=R/'artifacts/size-bif354-full-candidate4';P=R/'artifacts/size-bif354-full4-preparation/composition-plan.json'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();S=lambda b:hashlib.sha256(b).hexdigest().upper();J=lambda p:json.loads(p.read_bytes())
assert H(P)=='C209F4B6A4B97655D70E5AB794334554955502C998B67DB428BA5C6827D72653'
sourcefreeze=R/'artifacts/size-bif354-full4-preparation/source-freeze.json';assert H(sourcefreeze)=='1630ACA2C8C983122F69989BDBE2718106A37D82CC2073186E4B35FC450E4EEB'
for row in J(sourcefreeze)['Files']:
 q=sourcefreeze.parent/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
plan=J(P);assert len(plan['Files'])==3211 and len(plan['StableClosure'])==3210 and len(plan['OwnedPaths'])==134
assert plan['RuntimeSourceSha256']=='F2BBB954CE212ED87AE16FE8F50ACA14268A9A5C9A06812E11799EB7C270AB58'
assert (A/'composition-plan.json').read_bytes()==P.read_bytes()
for row in plan['InputPins']:
 q=R/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
for row in plan['StableClosure']:
 q=C/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'],row['Path']
assert J(A/'stable-closure-after.json')==dict(VerifiedRows=3210,PlanSha256=H(P),Unchanged=True)
inputs=J(A/'inputs.json');assert inputs['PlanSha256']==H(P) and Path(inputs['Candidate']).resolve()==C.resolve()
runner=R/'artifacts/size-bif354-full4-preparation/run-full.py';assert H(runner)=='B171AC0E354561565AB8BBED4D703B11BBF54B108ADF86372F24407578051195' and inputs['RunnerSha256']==H(runner)
assert inputs['InputRows']==3211 and inputs['StableRows']==3210 and inputs['ExpectedTests']==7343 and inputs['OldOrderedPrefix']==7342 and inputs['Attempt']==4
assert inputs['RegistryWillNotBeRestored'] and not inputs['OriginalOtpExecuted']
peer=A/'runner-source-review.json';assert H(peer)==inputs['SourcePeerSha256']=='D7A55B7EF3B6B38C70374E1044D3091DC4FC6016B2D052876D3BC3C117E05BAA'
helper=R/'artifacts/spawn-priority351-preparation/execute-root.py';assert H(helper)==inputs['OwnershipHelperSha256']=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
redgreen=A/'accepted-red-green.json';assert H(redgreen)==inputs['AcceptedRedGreenGuardSha256']=='D16B0CDA22E192AD57B84E836F2D8EDF698B63184376E81B3E5E8FFCCD3DF30B'
assert J(redgreen)['Status']=='PASS_DETERMINISTIC_REAL_ACTOR_RED_GREEN_CLEANUP_STEALING'
redgreenreview=R/'artifacts/halt-cleanup354-redgreen-independent/actual1-report.json';assert H(redgreenreview)=='5AA1C43D8FD60789C47CB852305F98A975218823E2BE8D58038E0B2B8D7B3709'
exit=J(A/'full.exit.json');live=J(A/'full.live.json');assert exit['ExitCode'] in (0,1) and exit['Settled'] and not exit['TimedOut'] and exit['Failures']==[] and exit['Pid']==live['Pid'] and exit['Command']==live['Command']
assert isinstance(exit['Pid'],int) and exit['Pid']>0 and 0<exit['ElapsedSeconds']<930 and exit['Command'][-1]==str(C/'tools/validate.ps1')
assert exit['StdoutSha256']==H(A/'full.stdout.log') and exit['StderrSha256']==H(A/'full.stderr.log')
passmode=exit['ExitCode']==0
captured=J(A/'captured-outputs.json');assert H(A/'raw-supported-mfas.json')==captured['RawRegistrySha256'] and not captured['RegistryRestored']
assert len(captured['Outputs'])==len({r['Path'] for r in captured['Outputs']})
for row in captured['Outputs']:
 q=A/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'] and q.read_bytes()==(C/row['Path']).read_bytes()
required={'artifacts/tests.json','artifacts/integration-results.json','artifacts/readiness/module-readiness.json','artifacts/readiness/MODULE_READINESS.md','artifacts/readiness/API_COMPATIBILITY.md','artifacts/tool-readiness/module-readiness.json'}
assert {r['Path'] for r in captured['Outputs']}<=required
if passmode:
 assert {r['Path'] for r in captured['Outputs']}==required
 assert J(A/'producer-completion.json')==dict(Status='ACTUAL_CANONICAL_FULL354_COMMAND_EXITED_ZERO_REQUIRES_INDEPENDENT_REVIEW',IndependentAcceptance=False,OriginalOtpExecuted=False,MainChanged=False)
else:assert not (A/'producer-completion.json').exists()
raw=J(A/'raw-supported-mfas.json');before=J(A/'registry-before.json');assert H(A/'registry-before.json')==plan['RegistryBefore']['Sha256']
assert len(raw)==len(before)==587 and len({(x['Module'],x['Function'],x['Arity']) for x in raw})==587 and all(isinstance(x['Limits'],str) for x in raw+before)
for actual,prior in zip(raw,before,strict=True):assert {k:v for k,v in actual.items() if k!='Limits'}=={k:v for k,v in prior.items() if k!='Limits'}
assert H(C/'docs/supported-mfas.json')==H(A/'raw-supported-mfas.json')
assert sum(x['Module']=='proc_lib' for x in raw)==39 and len({x['Module'] for x in raw})==14
ledger=J(C/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==115
counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']));assert counts=={'Verified compatible':16,'Known differences':30,'Needs verification':541}
for row in ledger['VerifiedSourceFiles']:assert row['Path'].startswith('src/') and H(C/row['Path'])==row['Sha256']
# Mirror strict ledger admission required fields before any PASS; hash identity alone is insufficient.
assert set(ledger)=={'SchemaVersion','Tag','Commit','VerifiedSourceFiles','Entries'} and ledger['SchemaVersion']==1
registered={(x['Module'],x['Function'],x['Arity']) for x in raw};seen=set()
for entry in ledger['Entries']:
 assert set(entry)=={'Module','Function','Arity','Status','Scope','Evidence','Limits'}
 assert isinstance(entry['Module'],str) and entry['Module'].strip() and isinstance(entry['Function'],str) and entry['Function'].strip() and type(entry['Arity']) is int and entry['Arity']>=0
 key=(entry['Module'],entry['Function'],entry['Arity']);assert key in registered and key not in seen;seen.add(key)
 assert isinstance(entry['Scope'],str) and entry['Scope'].strip() and isinstance(entry['Limits'],list) and all(isinstance(x,str) and x.strip() for x in entry['Limits']) and isinstance(entry['Evidence'],list)
 for proof in entry['Evidence']:
  assert set(proof)=={'Kind','Path','Sha256','Scope'} and proof['Kind'] in {'OriginalOtpComparison','CompatibilityAudit','SourceReview','LocalValidation'}
  assert all(isinstance(proof[k],str) and proof[k].strip() for k in ('Path','Sha256','Scope')) and re.fullmatch('[A-Fa-f0-9]{64}',proof['Sha256'])
  assert not PurePosixPath(proof['Path']).is_absolute() and ':' not in proof['Path'] and all(p not in ('','.','..') for p in re.split(r'[/\\]',proof['Path']))
 if entry['Status']=='Verified compatible':assert not entry['Limits'] and {'OriginalOtpComparison','CompatibilityAudit'} <= {x['Kind'] for x in entry['Evidence']}
 else:assert entry['Limits'] and (entry['Status']!='Known differences' or entry['Evidence'])
assert seen==registered
evidence={}
for entry in ledger['Entries']:
 for row in entry['Evidence']:assert H(C/'docs'/row['Path'])==row['Sha256'];evidence[row['Path']]=row['Sha256']
oldpassed=J(A/'previous-tests.json');oldregistered=J(A/'previous-registered-tests.json');assert oldpassed['Passed']==7313 and oldpassed['Failed']==0 and len(oldpassed['Tests'])==7313
assert len(oldregistered['Tests'])==7342 and oldregistered['Passed']==7341 and oldregistered['Failed']==1
assert (A/'previous-tests.json').read_bytes()==(C/plan['PreviousTestReport']).read_bytes()
assert (A/'previous-registered-tests.json').read_bytes()==(R/plan['PreviousAllTestNamesSource']).read_bytes()
tests=J(A/'artifacts/tests.json') if (A/'artifacts/tests.json').is_file() else None;failures=[];passed=None;failed=None;oldprefix=False
if tests is not None:
 names=[x['Name'] for x in tests['Tests']];assert len(names)==len(set(names))==7343 and names[:7342]==[x['Name'] for x in oldregistered['Tests']] and names[:7313]==[x['Name'] for x in oldpassed['Tests']]
 assert names[7342:]==plan['AddedTestNames']==['runtime/canceled-unbound-continuation-stolen-before-bound-blocker-release']
 assert all(x['Status'] in ('Passed','Failed') for x in tests['Tests']);passed=sum(x['Status']=='Passed' for x in tests['Tests']);failed=7343-passed;assert tests['Passed']==passed and tests['Failed']==failed
 failures=[x for x in tests['Tests'] if x['Status']=='Failed'];oldprefix=True
if passmode:assert tests is not None and passed==7343 and failed==0
expectedintegrations=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed')
integrations=J(A/'artifacts/integration-results.json') if (A/'artifacts/integration-results.json').is_file() else None
if passmode:assert integrations==expectedintegrations
readiness=[]
for path in ('artifacts/readiness/module-readiness.json','artifacts/tool-readiness/module-readiness.json'):
 if (A/path).exists():
  v=J(A/path)
  if passmode:
   assert v['RegisteredMfas']==587 and v['VerifiedCompatibleMfas']==16 and v['KnownDifferenceMfas']==30 and v['NeedsVerificationMfas']==541 and v['LocalPassedTests']==7343 and v['ImplementedApiModules']==14
   assert v['VerifiedSourceFiles']==ledger['VerifiedSourceFiles'] and v['ComparisonTestReport']=='validation/part-353/full/raw/artifacts/tests.json'
  readiness.append(dict(Path=path,Sha256=H(A/path)))
packageinputs={Path(r['Path']).name:C/r['Path'] for r in plan['PackageResources']};assert len(packageinputs)==6 and H(packageinputs['OTP-PORTS.md'])=='BF72D0AEDA38823EAF03A8DCE3F4956369AD0EAD61E366DB5026C9B2D030E4E5'
packages=[];ids={'Erlang.Net.CSharp','Erlang.Net.Tool'}
assert {r['Path'] for r in captured['Packages']}=={p.relative_to(C).as_posix() for p in (C/'artifacts/packages').glob('*.nupkg')}
for row in captured['Packages']:
 q=C/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'];checked=[]
 with zipfile.ZipFile(q) as z:
  files=[x for x in z.namelist() if not x.endswith('/')];assert len(files)==len(set(files)) and z.testzip() is None and all(not PurePosixPath(x).is_absolute() and '..' not in PurePosixPath(x).parts and '\\' not in x for x in files)
  ns=[x for x in files if x.endswith('.nuspec')];assert len(ns)==1;tree=ET.fromstring(z.read(ns[0]));values={n.tag.split('}')[-1]:n.text for n in tree.iter() if n.tag.split('}')[-1] in ('id','version')};assert values['id'] in ids and values['version']=='0.1.0';ids.remove(values['id'])
  for name,path in packageinputs.items():
   member='README.md' if name=='README.md' else 'licenses/'+name;assert z.read(member)==path.read_bytes();checked.append(member)
  projects=['Erlang.Terms','Erlang.Runtime','Erlang.Compiler','Erlang.Otp.Behaviours','Erlang.Serialization'] if values['id']=='Erlang.Net.CSharp' else ['Erlang.Tool','Erlang.Terms','Erlang.Runtime','Erlang.Compiler']
  for project in projects:
   member=('lib/net10.0/' if values['id']=='Erlang.Net.CSharp' else 'tools/net10.0/any/')+project+'.dll';path=C/('src' if values['id']=='Erlang.Net.CSharp' else 'tools')/project/'bin/Debug/net10.0'/(project+'.dll')
   if values['id']=='Erlang.Net.Tool':path=C/'tools/Erlang.Tool/bin/Debug/net10.0'/(project+'.dll')
   assert z.read(member)==path.read_bytes();checked.append(member)
  packages.append(dict(Path=row['Path'],Sha256=row['Sha256'],Id=values['id'],Version=values['version'],ZipMembers=len(files),CrcAndSafeUniquePathsVerified=True,SourceBoundMembers=checked))
if passmode:assert len(packages)==2 and not ids
childroot=C/'artifacts/term-order-children';children=captured['Children'];assert {r['Path'] for r in children}=={p.relative_to(C).as_posix() for p in childroot.glob('*/receipt.json')}
if tests is not None or passmode:assert len(children)==12
matrix={(f,o,32768) for f in ('tuple','list-head','map-value','fun-environment','native-record') for o in ('compare','hash')}|{('tuple','shallow',6),('tuple','snapshot',0)}
observed=[];terms=C/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Terms.dll';driver=C/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll'
for row in children:
 p=C/row['Path'];assert H(p)==row['Sha256'];v=J(p)
 assert v['Started'] and v['Settled'] and not v['TimedOut'] and not v['KillRequested'] and v['ExitCode']==0 and not v['LifecycleFailures'] and v['StandardOutputComplete'] and v['StandardErrorComplete'] and isinstance(v['ProcessId'],int) and v['ProcessId']>0 and 0<=v['ElapsedSeconds']<=60
 assert Path(v['DriverPath']).resolve()==driver.resolve();d=p.parent;assert H(d/'stdout.txt')==v['StandardOutputSha256'] and H(d/'stderr.txt')==v['StandardErrorSha256'] and (d/'stderr.txt').read_bytes()==b''
 lines=(d/'stdout.txt').read_text(encoding='utf-8-sig').splitlines();loaded=json.loads(lines[0]);assert Path(loaded['TermsAssemblyPath']).resolve()==terms.resolve() and loaded['TermsAssemblySha256']==v['TermsDllSha256']==H(terms) and lines[-1]=='PASS'
 if v['Operation'] in ('compare','hash'):assert 'CONSTRUCTED' in lines
 observed.append((v['Family'],v['Operation'],v['Depth']))
if tests is not None or passmode:assert len(observed)==len(set(observed))==12 and set(observed)==matrix
nativeplans=[]
for path,wf,var,n in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1282),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1307)]:
 b=(C/path).read_bytes();assert b'\r' not in b and not b.startswith(b'\xef\xbb\xbf');v=json.loads(b);assert len(v['Files'])==n
 for row in v['Files']:
  data=(C/row['Path']).read_bytes();assert row['HashMode'] in ('TextLF','BinaryExact');data=data.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else data;assert len(data)==row['Bytes'] and S(data)==row['Sha256']
 assert re.findall(var.encode()+rb': ([A-F0-9]{64})',(C/wf).read_bytes())==[H(C/path).encode()];nativeplans.append(dict(Path=path,Rows=n,Sha256=H(C/path),AllRowsAndSealVerified=True))
assert len(captured['TestDlls'])==len({r['Path'] for r in captured['TestDlls']}) and {r['Path'] for r in captured['TestDlls']}=={q.relative_to(C).as_posix() for q in (C/'tests/Erlang.Tests/bin/Debug/net10.0').glob('*.dll')}
for row in captured['TestDlls']:q=C/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
status='PASS_INDEPENDENT_ACTUAL_CANONICAL354_FULL4_RAW_REGISTRY_RESTORATION_PENDING' if passmode else 'PASS_INDEPENDENT_ACTUAL_FAILED_CANONICAL354_FULL4_ACQUISITION_NO_FULL_ACCEPTANCE'
result=dict(Status=status,Passed=passed,Failed=failed,FullAccepted=passmode,CompositionPlanSha256=H(P),SourceFreezeSha256=H(sourcefreeze),StableClosureRows=3210,OwnedPaths=134,Old7313ExactPrefix=oldprefix,Old7342ExactOrderedNamesPrefix=oldprefix,AddedTestNames=plan['AddedTestNames'],FailureRows=failures,IntegrationResults=integrations,Packages=packages,PackageInputs=6,TermOrderChildren=len(children),ChildMatrixVerified=set(observed)==matrix,RuntimeSourceGuards=115,CompatibilityCounts=counts,LedgerEvidenceRows=len(evidence),LedgerRequiredEvidenceSchemaVerified=True,Readiness=readiness,RegistryRows=587,RegistryNonLimitsAndOrderPreserved=True,RegistryBeforeSha256=H(A/'registry-before.json'),RawRegistrySha256=H(A/'raw-supported-mfas.json'),CustomLimitsRestorationPending=True,AcceptedRedGreenGuardSha256=H(redgreen),IndependentlyAcceptedRedGreenReviewSha256=H(redgreenreview),FullExitSha256=H(A/'full.exit.json'),FullStdoutSha256=H(A/'full.stdout.log'),FullStderrSha256=H(A/'full.stderr.log'),TestReportSha256=H(A/'artifacts/tests.json') if tests is not None else None,CapturedIndexSha256=H(A/'captured-outputs.json'),NativeSourcePlans=nativeplans,ReaderSha256=H(Path(__file__)),OriginalOtpExecuted=False,MainModified=False,PriorAttemptsNotOverwritten=True,DriverIdentityScope='Recorded driver path/current hash; only Terms identity emitted by child; no initial fullDLLsnapshot claimed.',NoCompatibilityPromotionInferredFromFull=True)
out=O/'actual-report.json';assert not out.exists();out.write_bytes((json.dumps(result,indent=2)+'\n').encode());print(H(out))
