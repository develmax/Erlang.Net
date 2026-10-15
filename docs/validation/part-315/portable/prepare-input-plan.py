"""Explicit frozen315 input index; passive hashes only, no archive created."""
from pathlib import Path
import hashlib,json,datetime
ROOT=Path.cwd();HERE=ROOT/'artifacts/root-part315-portable1';A=ROOT/'artifacts';ROWS=[];LOGICAL=set();CHUNK=1024*1024

def digest(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(CHUNK),b''):h.update(b)
 return h.hexdigest().upper()
def write(p,v):p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
def load(p):return json.loads(p.read_bytes())
def add(p,logical,role,expected=None):
 p=p.resolve();assert p.is_relative_to(ROOT) and not p.is_relative_to(ROOT/'docs');assert logical not in LOGICAL,logical
 value=digest(p);assert expected is None or value==expected,(p,value,expected)
 ROWS.append({'SourcePath':p.relative_to(ROOT).as_posix(),'LogicalPath':logical,'Role':role,'Bytes':p.stat().st_size,'Sha256':value});LOGICAL.add(logical)
def family(name,role):
 p=A/name
 for f in sorted(p.rglob('*')):
  if f.is_file():
   assert not {'bin','obj','__pycache__','.git'}.intersection(f.relative_to(p).parts)
   add(f,'source-only/'+name+'/'+f.relative_to(p).as_posix(),role)
assert not (HERE/'input-plan.json').exists()
roles=[('native-payload-provider315-composed1','Core315ActualCompiled','AB9E5CE828DEA20C28AB3BA93F17D9F5B8B422DEAAB9549E22A01C7EAD127A13','3B84468C8E72C255D9CF788E2A3E9245B34B7BF4EE916BED5593174826DBB108',2224),('native-payload-provider315-composed2','C315ActualCompiledAndWindowsNative','E06E71801D4BABA6B737C522F6DAA94D44042F5AC2A98091B22B3F035F3F66BB','3883365D8AAC2F8F129A735A8557410E331B3C45DECF556C554A4A93C19ACE1C',2227)]
for name,role,freeze,closure,count in roles:
 p=A/name;assert digest(p/'formatted-source-freeze.json')==freeze;assert digest(p/'formatted-source-resource-closure.json')==closure
 full=load(p/'formatted-source-resource-closure.json');assert len(full['Files'])==count
 for e in full['Files']:add(p/'candidate'/e['Path'],role+'/candidate/'+e['Path'],role+'SourceClosure',e['Sha256'])
 for f in sorted(p.iterdir()):
  if f.is_file():add(f,role+'/evidence/'+f.name,role+'DirectEvidence')
core=A/roles[0][0];c=A/roles[1][0]
focus=load(core/'focus-results.json');assert focus['Status']=='PASS' and len(focus['Cases'])==8
for e in focus['Identities']:assert digest(Path(e['Path']))==e['Sha256']
assert digest(core/'actual-focus-independent-guard.json')=='CC456D668364C4928D015D76DB064C32B9C041832C99683CADB86356770E20C1'
output=core/'candidate/tests/Erlang.Tests/bin/Debug/net10.0'
for p in sorted(output.rglob('*')):
 if p.is_file():add(p,'Core315ActualCompiled/candidate/tests/Erlang.Tests/bin/Debug/net10.0/'+p.relative_to(output).as_posix(),'Core315ActualTestsOutput')
runner=A/'source-purge-stage315-focus'
for n in ['Program.cs','Focus.csproj']:add(runner/n,'Core315ActualCompiled/focus-runner/'+n,'Core315FocusReplaySource')
for p in sorted((runner/'bin/Debug/net10.0').iterdir()):
 if p.is_file():add(p,'Core315ActualCompiled/focus-runner/bin/Debug/net10.0/'+p.name,'Core315FocusReplayBinary')
windows=A/'root-part315-isolated-windows1';assert digest(windows/'guard.json')=='D736ECFC0594FDD3C8DCB09D6355541CD11B978AC3326FDA10AF07CB379A775F'
independent=A/'root-part315-isolated-windows1-beam310-independent-guard.json';assert digest(independent)=='E432DD70BE5832D3B8942FF15FFBD4558771E5B75A98C6D44B35679624E4BE35'
for p in sorted(windows.iterdir()):
 if p.is_file():add(p,'C315ActualCompiledAndWindowsNative/windows-evidence/'+p.name,'C315ActualWindowsCapturesAndGuards')
add(independent,'C315ActualCompiledAndWindowsNative/windows-evidence/'+independent.name,'C315IndependentActualWindowsGuard')
output=c/'candidate/tools/Erlang.NifNativeBackend/bin/Debug/net10.0'
for p in sorted(output.rglob('*')):
 if p.is_file():add(p,'C315ActualCompiledAndWindowsNative/candidate/tools/Erlang.NifNativeBackend/bin/Debug/net10.0/'+p.relative_to(output).as_posix(),'C315ActualToolOutput')
trusted={}
for role in ['legacy','provider-query','reallocation']:
 attempt=load(windows/(role+'.attempt.json'))
 for e in attempt['Bindings']:
  p=ROOT/e['Path'];assert digest(p)==e['Sha256']
  if p.is_relative_to(output):continue
  trusted.setdefault(e['Path'],e)
assert len(trusted)==3, list(trusted)
for n,e in trusted.items():add(ROOT/n,'C315ActualCompiledAndWindowsNative/trusted-native/'+Path(n).name,'ActualTrustedNativeDllOrLayout',e['Sha256'])
for name,role in [
 ('native-payload-provider315-candidate1','SourceOnlyProviderAuthorA413'),('native-payload-provider315-c-harness-candidate1','SourceOnlyCQueryAuthorE1D'),('native-payload-provider315-design1','SourceOnlyProviderDesignReleaseAudit'),('native-payload-provider315-c-harness-design1','SourceOnlyCQueryDesign'),('native-payload-provider315-source-peer1','ProviderSourcePeer'),('native-payload-provider315-c-harness-allocator309-source-peer1','CQuerySourcePeer'),('native-payload-provider315-manual-reader1','SourceOnlyManualReader61SyntheticControls'),('native-payload-provider315-manual-reader-beam310-replay1','SourceOnlyManualReaderIndependentSyntheticReplay'),('source-plan-preflight315-candidate1','SourceOnlyPreflight10PureControls'),('source-plan-preflight315-beam310-replay1','SourceOnlyPreflightIndependentPureReplay')]:family(name,role)
# Design's exact historical18 source bindings resolved only in named frozen roots.
for e in load(A/'native-payload-provider315-design1/source-audit-freeze.json')['SourceBindings']:
 choices=[A/'allocator-query-mailbox314-import-policy2/candidate'/e['Path'],A/'allocator-query-mailbox314-composed1/candidate'/e['Path'],core/'candidate'/e['Path']]
 p=next((p for p in choices if p.is_file() and digest(p)==e['Sha256']),None);assert p is not None,e['Path']
 add(p,'source-only/provider-design-inputs/'+e['Path'],'HistoricalExactProviderDesignSourceBinding',e['Sha256'])
for n in [
 'root-focus315-provider.ps1','root-format315-provider.ps1','root-guard315-provider-focus.py','root-format315-c-harness.ps1','root-build315-c-harness.ps1','root-run315-isolated-windows.py','root-guard315-isolated-windows.py',
 'native-payload-provider315-beam310-formatted-peer.py','native-payload-provider315-beam310-formatted-source-peer.json','native-payload-provider315-composed2-beam310-source-peer.json','native-payload-provider315-manual-reader-beam310-peer.py','native-payload-provider315-manual-reader-beam310-source-peer.json','source-plan-preflight315-beam310-peer.py','source-plan-preflight315-beam310-source-peer.json']:
 add(A/n,'source-only/peer-and-capture-scripts/'+n,'PeerOrActualCaptureReplaySource')
for p in [HERE/'archive-reviewed.py',Path(__file__).resolve()]:add(p,'packaging/'+p.name,'PassivePackagingSource')
# Four separate raw314 API ZIPs, never part of this archive.
zips=[];evidence={};runs=[]
def remote(p,role):
 assert p.is_file() and not p.is_relative_to(ROOT/'docs');n=p.relative_to(ROOT).as_posix()
 value=evidence.setdefault(n,{'SourcePath':n,'SuggestedDestination':'docs/validation/part-315/previous-head314/'+n.removeprefix('artifacts/'),'Bytes':p.stat().st_size,'Sha256':digest(p),'Roles':[]})
 if role not in value['Roles']:value['Roles'].append(role)
for role,run in [('ci',38805548225),('reallocation-failure',38805899881),('native',38805923856),('ordinary',38805949831)]:
 d=A/('pack314-watch-'+str(run)+'-terminal');cap=load(d/'raw-capture.json');meta=load(d/'artifacts.json');assert cap['RunId']==run
 for e in meta['artifacts']:
  p=d/('artifact-'+str(e['id'])+'.zip');value=digest(p);assert e['digest'].lower()=='sha256:'+value.lower() and p.stat().st_size==e['size_in_bytes']<100*1024*1024
  n=p.relative_to(ROOT).as_posix();zips.append({'Role':role,'Run':run,'Artifact':e['id'],'Conclusion':cap['WorkflowConclusion'],'SourcePath':n,'SuggestedDestination':'docs/validation/part-315/previous-head314/'+n.removeprefix('artifacts/'),'Bytes':p.stat().st_size,'Sha256':value,'ApiDigestVerified':True})
 for p in sorted(d.iterdir()):
  if p.is_file() and p.suffix in {'.json','.log'}:remote(p,'RawTerminalEnvelope-'+role)
 runs.append({'Role':role,'Run':run,'Conclusion':cap['WorkflowConclusion']})
for p in sorted(A.glob('pack314-*')):
 if p.is_file() and p.suffix in {'.json','.py','.stdout','.stderr'}:remote(p,'IndependentRemoteAuditAndRawProof')
for p in sorted((A/'part314-readonly-monitor').iterdir()):
 if p.is_file():remote(p,'ReadOnlyDispatchMapping')
for name in ['pack314-original13-source-contracts','pack314-sdk-source-contracts','pack314-reallocation-source-contracts']:
 for p in sorted((A/name).iterdir()):
  if p.is_file():remote(p,'RemoteStaticSourceContract')
for n in ['root-part314-post-publication-dispatch-authority.json','root-part314-native-dispatch-attempt.json','root-part314-native-dispatch-result.json','root-part314-ordinary-dispatch-attempt.json','root-part314-ordinary-dispatch-result.json','root-part314-reallocation-dispatch-attempt.json','root-part314-reallocation-dispatch-result.json','root-part314-stage-index-guard.json','root-part314-stage-attempt.json','root-part314-stage-pathspec']:
 remote(A/n,'DispatchOrStagingBoundary')
remoteindex={'Status':'EXPLICIT_SEPARATE_PREVIOUS_HEAD314_COPY_INDEX_NO_COPY','Head':'91b0e8ca650937283eb0beb3b3eed0b88bbbe423','RawApiZipRows':zips,'RawJsonLogsAndReviewRows':sorted(evidence.values(),key=lambda e:e['SourcePath']),'Runs':runs,'DeferredRuns':[],'NoUnpackedArtifactsOrRuntimeTarIndexed':True,'CombinedRemoteZipCreated':False,'MainDocsCopied':False}
write(HERE/'remote-separate-copy-index.json',remoteindex)
# Explicit finaldocs copy rows, with one uniformly stripped artifacts/ prefix.
copy=[{'SourcePath':e['SourcePath'],'DestinationPath':e['SuggestedDestination'],'Bytes':e['Bytes'],'Sha256':e['Sha256']} for e in zips+list(evidence.values())]
write(HERE/'remote-finaldocs-copy-manifest.json',sorted(copy,key=lambda e:e['SourcePath']))
groups={};blobs={}
for e in ROWS:
 g=groups.setdefault(e['Role'],{'Rows':0,'Bytes':0});g['Rows']+=1;g['Bytes']+=e['Bytes'];blobs[e['Sha256']]=e['Bytes']
plan={'Status':'REVIEW_REQUIRED_NO_ARCHIVE_CREATED','CreatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'PartDocumentationDateMoscow':'2026-10-16','Scope':'Two distinct actual315 roles: Core AB9E2224 target/focus8 and C E06E2227 Tool target/three trusted Windows captures. Source-only author/design/pure-control roles retained separately. Not Main full315 completion, Linux execution or public allocator API proof.','ArchiveContainsRemoteEvidence':False,'MaximumArchiveBytes':100*1024*1024,'ArchiveScriptSha256':digest(HERE/'archive-reviewed.py'),'SourceRoles':[{'Role':role,'SourceRows':count,'FreezeSha256':freeze,'ClosureSha256':closure} for _,role,freeze,closure,count in roles],'ActualCoreIdentities':focus['Identities'],'ActualTrustedNativeBindings':list(trusted.values()),'ReplayRequirements':'Materialize each logical path from its SHA-addressed blob; requires external .NET10 and supported original Windows environment/native architecture. Source profile metadata and included native DLLs are trusted fixture evidence, not general NIF support. No code runs automatically. New SDK counters have no original OTP oracle run and do not measure physical memory; C intermediate realloc extent not observed.','Groups':groups,'LogicalRows':len(ROWS),'LogicalBytes':sum(e['Bytes'] for e in ROWS),'UniqueBlobs':len(blobs),'UniqueBlobBytes':sum(blobs.values()),'RemoteCopyIndex':{'Path':(HERE/'remote-separate-copy-index.json').relative_to(ROOT).as_posix(),'Sha256':digest(HERE/'remote-separate-copy-index.json'),'RawZipRows':len(zips),'EvidenceRows':len(evidence)},'ArchiveRows':ROWS}
write(HERE/'input-plan.json',plan)
print(json.dumps({k:v for k,v in plan.items() if k not in ['ArchiveRows','Groups','ActualCoreIdentities','ActualTrustedNativeBindings']},indent=2));print('InputPlanSha256',digest(HERE/'input-plan.json'));print('Groups',json.dumps(groups))
