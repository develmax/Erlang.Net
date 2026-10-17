from pathlib import Path
import ast,hashlib,json,re
R=Path(__file__).resolve().parents[2];P=R/'artifacts/spawn-priority351-preparation';C=R/'artifacts/spawn-priority351-candidate';O=Path(__file__).resolve().parent
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
f=P/'styled-source-freeze.json';assert h(f)=='AA468252959B4C0229410DC09673C9219277FF9B02E1AD63F06897DEAFCD9C2D'
source=rd(f);assert len(source['Files'])==38
for x in source['Files']:
 a=C/x['Path'];b=P/'styled-source'/x['Path'];assert a.stat().st_size==x['Bytes'] and h(a)==h(b)==x['Sha256']
closure=P/'source-resource-closure.json';assert h(closure)==source['SourceClosureSha256']=='E61E509F1279AEA447A9DADD86BA3C1A65B53E7F4C4055666E59A27BB66DF955'
assert len(rd(closure)['Files'])==2798
for x in rd(closure)['Files']+source['PackageReadinessInputs']:
 a=C/x['Path'];assert a.stat().st_size==x['Bytes'] and h(a)==x['Sha256']
prod=rd(P/'runtime-styled-source-freeze2.json')['Files'];assert len(prod)==20
for x in prod:assert h(C/x['Path'])==x['Sha256']
targets=rd(P/'focus-targets.json');assert h(P/'focus-targets.json')==source['TargetManifestSha256']
assert len(targets['Targets'])==len(set(targets['Targets']))==24
assert (targets['NewHost'],targets['NewPortable'],targets['ExistingHost'],targets['ExistingCompiled'],targets['ParseSourceCount'])==(8,3,6,7,10)
for x in targets['ExistingSourceBindings']:assert h(C/x['Path'])==x['Sha256']
focus=C/'tests/Erlang.Priority351.Focus';proposal=R/'artifacts/spawn-priority351-proposal3'
new_constants={}
for name in ['SpawnPriorityHostNames','SpawnPriorityLifecycleHostNames','SpawnPriorityReentryNames']:
 old=(proposal/'host-proposals'/(name+'.cs')).read_text();new=(focus/(name+'.cs')).read_text()
 q=r'internal const (?:string|int) (\w+) = ("(?:[^"\\]|\\.)*"|\d+);'
 before=dict(re.findall(q,old));after=dict(re.findall(q,new))
 assert all(after[k]==v for k,v in before.items())
 for key,value in after.items():
  if key not in before:new_constants[name+'.'+key]=value
assert new_constants=={'SpawnPriorityHostNames.Delimiter':'","','SpawnPriorityHostNames.ExpectedChildren':'4','SpawnPriorityLifecycleHostNames.Delimiter':'","','SpawnPriorityLifecycleHostNames.QueuedChangeChildren':'2','SpawnPriorityLifecycleHostNames.SuccessfulHaltStatus':'0'}
for old in (proposal/'host-proposals').glob('*.cs'):
 if old.stem.endswith('Names'):continue
 a=old.read_text();b=(focus/old.name).read_text()
 for key,value in new_constants.items():b=b.replace(key,value)
 assert re.sub(r'\s+','',a)==re.sub(r'\s+','',b),old.name
portable=list((proposal/'portable-proposals').glob('*.erl'));assert len(portable)==3
for old in portable:assert old.read_bytes()==(focus/'Portable'/old.name).read_bytes()
helper=P/'execute-root.py';assert h(helper)==source['ExecutionHelperSha256']=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A';ast.parse(helper.read_text())
v=dict(Status='PASS_INDEPENDENT_PRIORITY351_FOCUSED_RUNNER24_SOURCE_REVIEW_ONLY',SourceFreezeSha256=h(f),SourceClosureSha256=h(closure),SourceRows=38,ProductionRows=20,BuildClosureRows=2798,PackageReadinessRows=len(source['PackageReadinessInputs']),ProductionRenamePeerSha256=h(O/'rename2-review.json'),Source3SemanticPeerSha256='8D96FAA8337AD4EEBE2CE0104A8E3D170A51F750FD442DDA15B5BBA94383D0FB',ExecutionHelperSha256=h(helper),TargetManifestSha256=h(P/'focus-targets.json'),Targets=targets['Targets'],HostConstantsExtraction=new_constants,HostAssertionsUnchangedAfterExactConstantSubstitution=True,ThreePortableSourceFilesRawExactProposal=True,ExistingRegressionSourceBindingsRehashed=True,RunnerReview=['All closure/readiness/target/helper hashes checked before fresh output creation; existing output cannot be reused.','Fresh build bounded240s, ParseAll10 bounded45s, each24target owns a fresh90s child with raw stdout/stderr, PID, kill-tree-and-join path and settled exit receipt.','Timeout/start/cleanup/nonzero failures refuse without fabricated pass; output raw files/receipts retained.','24 unique named delegates are8new HOST+3new compiled+6exact existing HOST+7exact catalog instances, including six named/anonymous recursion/cleanup modules and hibernate.','ReferenceResourceHostChecks.Run is reflected from original built Tests assembly, retaining embedded resources; no mirrored fake resource control.','Compiled cases use original Expected, actual CompiledModuleExecution result and real artifact.CSharpSha256. ParseAll precedes any target; actual report must match same10source identities.','Loaded assembly hashes cover actual8type-owning assemblies and must resolve under fresh build output.','Production20 unchanged from reviewed rename2 F8A3; HOST changes beyond style explicitly limited to5same-valued constants, verified by exact substitutions.','Private metadata ledger59 is intentionally stale for modified source; no reporting/full/readiness target or status promotion.'],BlockingSourceFindings=[],BoundedFocusedExecutionAdmissible=True,RemainingGates=['Actual build/parser/24results and all owned process cleanup must settle; source review is not observed success.','Actual cancellation/resource/image/deep regressions and lowepoch control outcomes must be inspected, not inferred.','No whole spawn_opt/proc_lib or native scheduler parity follows from this focused set.'],SdkExecuted=False,NativeExecuted=False,MainModified=False,NetworkUsed=False)
out=O/'focused-runner-review.json';assert not out.exists();out.write_text(json.dumps(v,indent=2)+'\n',newline='\n');print(h(out))
