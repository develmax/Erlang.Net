"""Root-only two-phase finalization; no build, VM, Git mutation or network execution."""
from pathlib import Path
import argparse,copy,hashlib,json,re,subprocess
R=Path.cwd().resolve();D=Path(__file__).resolve().parent
H=lambda b:hashlib.sha256(b).hexdigest().upper();J=lambda p:json.loads(p.read_bytes());E=lambda v:(json.dumps(v,indent=2)+'\n').encode()
def exact(p,h):
 assert re.fullmatch('[A-Fa-f0-9]{64}',h) and H(p.read_bytes())==h.upper(),str(p)
 return p.read_bytes()
def bound(x):
 p=(R/x['Path']).resolve();assert p.is_relative_to(R);b=exact(p,x['Sha256'])
 if 'Bytes' in x:assert len(b)==x['Bytes']
 return p
def snapshot(n):
 p=R/n;return dict(Path=n,Exists=p.exists(),Bytes=p.stat().st_size if p.exists() else None,Sha256=H(p.read_bytes()) if p.exists() else None)
def row(p,target):
 b=p.read_bytes();return dict(SourcePath=p.resolve().relative_to(R).as_posix(),TargetPath='docs/validation/part-329/'+target,Bytes=len(b),Sha256=H(b))
p=argparse.ArgumentParser();p.add_argument('phase',choices=['scope','finalize'])
for role in ['main-source-freeze','full-guard','native-guard','full-guard-source','finalizer-source-peer','late-proof-rows']:
 p.add_argument('--'+role,required=True,type=Path);p.add_argument('--'+role+'-sha256',required=True)
for role in ['closure','author-final-freeze','source-peer']:p.add_argument('--'+role+'-sha256',required=True)
p.add_argument('--readiness-sha256');p.add_argument('--readiness-exit-sha256');a=p.parse_args()
for x in J(D/'proposal-freeze.json')['Files']:assert len(exact(D/x['Path'],x['Sha256']))==x['Bytes']
cfg=J(D/'configuration.json');art=R/'artifacts';output=R/'docs/validation/part-329';control=art/'root-part329-docs1'
fp,fullp,nativep,guardsource,finalpeer,latep=[x.resolve() for x in [a.main_source_freeze,a.full_guard,a.native_guard,a.full_guard_source,a.finalizer_source_peer,a.late_proof_rows]]
for q in [fp,fullp,nativep,guardsource,finalpeer,latep]:assert q.is_relative_to(art)
assert fp.parent==fullp.parent and fp.name=='source-freeze.json' and fp.parent.name=='root-part329-main1'
for q,h in [(fp,a.main_source_freeze_sha256),(fullp,a.full_guard_sha256),(nativep,a.native_guard_sha256),(guardsource,a.full_guard_source_sha256),(finalpeer,a.finalizer_source_peer_sha256),(latep,a.late_proof_rows_sha256)]:exact(q,h)
assert J(finalpeer)['Status'].startswith('PASS') and H((D/'proposal-freeze.json').read_bytes()) in json.dumps(J(finalpeer))
source,full,native=J(fp),J(fullp),J(nativep);closure=fp.parent/'source-resource-closure.json';exact(closure,a.closure_sha256)
assert source['ParentHead']==cfg['ParentHead'] and source['FinalSourceClosureSha256']==a.closure_sha256.upper()
assert (source['SourcePaths'],source['CSharpPaths'],source['SourceClosurePaths'],source['ExpectedFullPassed'])==(12,7,2397,6747)
assert source['AuthorFinalFreeze']['Sha256']==a.author_final_freeze_sha256.upper() and source['IndependentPeer']['Sha256']==a.source_peer_sha256.upper()
author=J(bound(source['AuthorFinalFreeze']));peer=J(bound(source['IndependentPeer']))
assert peer['Status'].startswith('PASS') and a.author_final_freeze_sha256.upper() in json.dumps(peer) and a.closure_sha256.upper() in json.dumps(peer)
assert author['SevenCompiledGreenCSharpRawExact'] and author['AllNativeNonFilesAndPathSetsExactParent']
assert author['GeneralClrStackSafetyClaimed'] is False and author['HistoricalFailuresRemainSeparateNotTestsPassed'] is True
inputs=J(closure)['Files'];assert len(inputs)==len({x['Path'] for x in inputs})==2397
for x in inputs:bound(x)
assert full['Status']=='PASS_ACTUAL_MAIN329_6747_FULL_AND_ELEVEN_INTEGRATIONS'
assert (full['Passed'],full['Failed'],full['IntegrationResults'],full['Registry'],full['ErlangMfas'])==(6747,0,11,509,351)
assert full['SourceFreezeSha256']==H(fp.read_bytes()) and full['SourceClosureSha256']==H(closure.read_bytes())
assert full['GuardSourceSha256']==a.full_guard_source_sha256.upper() and J(fp.parent/'full-exit.json')['ExitCode']==0
for k in ['All6738PreviousOrderedNamesPreserved','AllThirtyCustomLimitsExactParent','AllRegistryRowsExactParent','NoMfaLimitsScopeAppended']:assert full[k] is True,k
tests=J(R/'artifacts/tests.json');old=J(R/'docs/validation/part-328/final-main/tests.json');added=set(full['AddedTestNames'])
assert tests['Passed']==len(tests['Tests'])==6747 and tests['Failed']==0 and len(added)==9 and all(x['Status']=='Passed' for x in tests['Tests'])
assert [x['Name'] for x in tests['Tests'] if x['Name'] not in added]==[x['Name'] for x in old['Tests']] and old['Passed']==6738
assert J(R/'artifacts/integration-results.json')==J(R/'docs/validation/part-328/final-main/integration-results.json') and len(J(R/'artifacts/integration-results.json'))==11
assert native['Status']=='PASS_ACTUAL_MAIN329_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE'
assert (native['FullPassed'],native['SourceFilesRehashed'],native['OriginalExecuted'],native['PhysicalMemoryCompared'])==(6747,2397,False,False)
assert native['SourceFreezeSha256']==H(fp.read_bytes()) and native['ActualFullGuard']['Sha256']==H(fullp.read_bytes())
assert native['ActualTrustedCQueryExecuted'] and native['EveryRecursiveToolDllJsonBoundAcrossAllThreeAttempts'] and native['MainExecution']
assert {x['Role'] for x in native['Roles']}=={'legacy','provider-query','reallocation'} and len(native['Roles'])==3
for x in native['Roles']:
 assert x['ExitCode']==0;bound(x['Attempt']);process=J(bound(x['Process']));assert process['ExitCode']==0 and process['TimedOut'] is False
bound(native['ActualTool']);bound(native['ActualRuntime'])
assert J(R/'docs/supported-mfas.json')==J(fp.parent/'registry-before-full.json')
registry=J(R/'docs/supported-mfas.json');assert len(registry)==509 and sum(x['Module']=='erlang' for x in registry)==351
assert sorted(x['Inputs'] for x in source['Plans'])==[1120,1145]
for plan in source['Plans']:
 raw=exact(R/plan['Path'],plan['Sha256']);assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf')
 assert len(json.loads(raw)['Files'])==plan['Inputs']
 for x in json.loads(raw)['Files']:
  b=(R/x['Path']).read_bytes();assert x['HashMode'] in ['TextLF','BinaryExact'];b=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b;assert len(b)==x['Bytes'] and H(b)==x['Sha256']
 assert re.findall(r'^\s*'+plan['Pin']+r': ([A-F0-9]{64})$',(R/plan['Workflow']).read_text(),re.M)==[plan['Sha256']]
proofs={x['Role']:x for x in author['ActualProofs']};assert set(proofs)=={'green-guard','managed-guard','retained-original-guard','interpreted-guard','historical-guard','source-peer'}
for role,x in proofs.items():
 v=J(bound(x));assert v['Passed'] is True and v['Status'].startswith('PASS')
 if role!='historical-guard':assert author['GreenSourceFreezeSha256'] in json.dumps(v)
 for b in v.get('Bindings',[]):bound(b)
green=J(bound(proofs['green-guard']));assert (green['HostGroupsPassed'],green['GeneratedCasesPassed'],green['SourceClosureFiles'])==(10,8,2397)
ledger=J(bound(source['ActualCaseLedger']));assert ledger['Passed'] and len(ledger['HostActuals'])==10
managed=J(bound(ledger['ManagedActual']));assert managed['RecipePassed'] and (managed['Executed'],managed['FixturePassed'],managed['FixtureFailed'])==(8,8,0)
for x in ledger['HostActuals']:assert J(bound(x))['Status']=='PASS'
for x in cfg['ClosedVolumes']:bound(x)
late=J(latep);assert isinstance(late,list) and late,'Explicit nonempty selected329 proof rows required'
static=J(bound(cfg['StaticRows']));assert len(static)==642
summary=J(bound(cfg['Prior328Summary']));assert summary['Passed'] and summary['Head']==cfg['ParentHead']
all_static=static+cfg['ThinPriorRows']+late
def prepare_copies(rows):
 copies={};ordered=[]
 for x in rows:
  q=(R/x['SourcePath']).resolve();target=(R/x['TargetPath']).resolve();assert q.is_relative_to(R) and target.is_relative_to(output)
  assert q.suffix.lower() not in ['.dll','.zip','.tar','.gz','.so','.exe','.pdb'] and target.name not in ['evidence-index.json','evidence-receipt.json']
  b=exact(q,x['Sha256']);assert len(b)==x['Bytes'] and len(b)<12*1024*1024
  if target in copies:assert copies[target]==b and next(y for y in ordered if y['TargetPath']==x['TargetPath'])==x
  else:copies[target]=b;ordered.append(x)
 return copies,ordered
prepare_copies(all_static)
assert not output.exists();scopepath=R/'docs/module-readiness-scope.json';scope=J(scopepath);note=(D/'runtime-note.txt').read_text().strip()
runtime=next(x for x in scope['Components'] if x['Id']=='Runtime')
if a.phase=='scope':
 assert not control.exists() and note not in runtime['Implemented'] and scope['ComparisonTestReport']=='validation/part-327/final-main/tests.json'
 before=[snapshot(n) for n in cfg['MetadataPaths']];oldscope=scopepath.read_bytes();runtime['Implemented'].append(note);scope['ComparisonTestReport']='validation/part-328/final-main/tests.json';updated=E(scope)
 command=dict(MetadataBefore=before,ScopeBeforeSha256=H(oldscope),ScopeAfterSha256=H(updated),MainFreezeSha256=H(fp.read_bytes()),FullGuardSha256=H(fullp.read_bytes()),NativeGuardSha256=H(nativep.read_bytes()),ScriptSha256=H(Path(__file__).read_bytes()),ConfigurationSha256=H((D/'configuration.json').read_bytes()),FinalizerSourcePeerSha256=H(finalpeer.read_bytes()),LateProofRowsSha256=H(latep.read_bytes()))
 control.mkdir();(control/'scope-before.json').write_bytes(oldscope);(control/'scope-command.json').write_bytes(E(command));scopepath.write_bytes(updated)
 print('Scope329 applied; Root must run actual readiness CLI before finalize.');raise SystemExit(0)
assert a.readiness_sha256 and a.readiness_exit_sha256;ready=R/'docs/module-readiness.json';exitp=fp.parent/'readiness-exit.json'
exact(ready,a.readiness_sha256);exact(exitp,a.readiness_exit_sha256);assert J(exitp)['ExitCode']==0
for k,v in dict(LocalPassedTests=6747,RegisteredMfas=509,PreviousRegisteredMfas=509,RegisteredMfaDelta=0,ComparisonTestReport='validation/part-328/final-main/tests.json').items():assert J(ready)[k]==v
command=J(control/'scope-command.json')
for k,v in dict(MainFreezeSha256=H(fp.read_bytes()),FullGuardSha256=H(fullp.read_bytes()),NativeGuardSha256=H(nativep.read_bytes()),ScriptSha256=H(Path(__file__).read_bytes()),ConfigurationSha256=H((D/'configuration.json').read_bytes()),FinalizerSourcePeerSha256=H(finalpeer.read_bytes()),LateProofRowsSha256=H(latep.read_bytes()),ScopeAfterSha256=H(scopepath.read_bytes()),ScopeBeforeSha256=H((control/'scope-before.json').read_bytes())).items():assert command[k]==v
expected=J(control/'scope-before.json');next(x for x in expected['Components'] if x['Id']=='Runtime')['Implemented'].append(note);expected['ComparisonTestReport']='validation/part-328/final-main/tests.json';assert scope==expected
assert next(x for x in J(ready)['Components'] if x['Id']=='Runtime')['Implemented'].count(note)==1
for x in command['MetadataBefore']:
 if x['Path'] in cfg['HumanFiles']+['CHANGELOG.md','docs/changelog/0051.md','docs/supported-mfas.json']:assert snapshot(x['Path'])==x
draft=exact(D/'english-entry.md',cfg['EnglishSha256']).decode();entry=draft
entry=entry.replace('Status: DRAFT. The isolated revision3 checks are in progress. Corrected final source, actual Main full validation, post-full native roles, readiness and independent cached-stage review remain mandatory future gates. No completed result is created by this entry or its reserved proof links.','Status: actual final source, focused and historical receipts, Main full, post-full native and readiness checks are bound below. Independent cached-stage review remains the next publication gate.')
entry=entry.replace('Planned focused outcome:','Actual focused outcome:')
entry=entry.replace('__ACTUAL_FOCUS_RESULT__','ten HOST selectors and eight generated ordinary cases passed; retained original input and module-null interpreted checks passed separately; cancellation uses actual Unwound before lease-zero checks and observes guest cleanup zero without claiming the after AST was never entered')
entry=entry.replace('__ACTUAL_MAIN_FULL_RESULT__','6747/6747 with eleven integrations/packages and all6738 previous ordered names preserved')
entry=entry.replace('This arithmetic is not an observed pass.','The actual full guard confirms this increase.')
entry=entry.replace('__ACTUAL_NATIVE_RESULT__','three fresh separate trusted Windows SDK roles passed after the actual full gate')
entry=entry.replace('__ACTUAL_READINESS_RESULT__','CLI exit0,509 total/351 erlang, previous509/delta0 against immutable328')
entry=entry.replace('After actual binding, the','The').replace('must name the exact current source and process evidence.','name the exact current source and process evidence.')
entry=entry.replace('Next, bind the actual329 gates and publish the bounded increment; then collect','Next, complete the independent cached-stage/publication checks, then collect')
assert '__ACTUAL_' not in entry and 'Status: DRAFT' not in entry
volume=(R/'docs/changelog/0051.md').read_bytes()+b'\n\n'+entry.strip().encode()+b'\n';assert len(volume)<=32768 and len(volume.splitlines())<=400
changes={R/'docs/changelog/0051.md':volume}
for name in cfg['HumanFiles']:
 raw=(R/name).read_bytes();assert note.encode() not in raw;nl=b'\r\n' if b'\r\n' in raw else b'\n';at=raw.index(nl*2)+len(nl)*2;changes[R/name]=raw[:at]+note.encode()+nl*2+raw[at:]
idx=(R/'CHANGELOG.md').read_bytes();oldline=b'- [0051.md](docs/changelog/0051.md)';assert idx.count(oldline)==1
changes[R/'CHANGELOG.md']=idx.replace('— part328 (active)'.encode(),'— parts328–329 (active)'.encode());assert changes[R/'CHANGELOG.md']!=idx
additions=[(fp,'final-main/source-freeze.json'),(closure,'final-main/source-resource-closure.json'),(fullp,'final-main/full-guard.json'),(nativep,'final-native/guard.json'),(ready,'module-readiness.json'),(guardsource,'final-main/full-guard-source.py'),(finalpeer,'final-author/finalizer-source-peer.json'),(latep,'final-author/late-proof-rows.json'),(bound(source['AuthorFinalFreeze']),'final-source/author-final-freeze.json'),(bound(source['IndependentPeer']),'final-source/independent-composition-peer.json'),(bound(source['ActualCaseLedger']),'final-source/actual-case-ledger.json')]
for role,x in proofs.items():additions.append((bound(x),'focus/summary.json' if role=='green-guard' else 'historical/summary.json' if role=='historical-guard' else 'focus/'+role+'.json'))
for x in source['Files']:additions.append((R/x['Path'],'final-source/owned/'+x['Path']))
for name in ['full-exit.json','full-attempt.json','full-validation.log','raw-supported-mfas.json','registry-before-full.json','readiness-exit.json','readiness-cli.log']:additions.append((fp.parent/name,'final-main/'+name))
for name in ['tests.json','integration-results.json']:additions.append((R/'artifacts'/name,'final-main/'+name))
for role in ['legacy','provider-query','reallocation']:
 for suffix in ['attempt.json','process.json','stdout','stderr']:additions.append((nativep.parent/(role+'.'+suffix),'final-native/'+role+'.'+suffix))
for name in ['scope-before.json','scope-command.json']:additions.append((control/name,'final-author/'+name))
for name in ['finalize329.py','configuration.json','english-entry.md','runtime-note.txt','proposal-freeze.json','README.md']:additions.append((D/name,'final-author/'+name))
copies,ordered=prepare_copies(all_static+[row(q,target) for q,target in additions])
for link in re.findall(r'\[[^\]]+\]\(([^)]+)\)',entry):
 if link.startswith('../validation/part-329/'):
  target=(R/'docs/changelog'/link).resolve();assert target in copies or target==output/'evidence-receipt.json'
# Read-only clean conversion models the prospective fourteen metadata files before writes.
changed_metadata=[];metadata_git=[]
for name in cfg['MetadataPaths']:
 raw=changes.get(R/name,(R/name).read_bytes())
 attr=subprocess.check_output(['git','--no-optional-locks','check-attr','filter','--',name],cwd=R,text=True)
 assert attr.rstrip().rsplit(': ',1)[-1] in ['unspecified','unset'],name
 clean=subprocess.check_output(['git','--no-optional-locks','hash-object','--path='+name,'--stdin'],cwd=R,input=raw).strip().decode()
 previous=subprocess.run(['git','--no-optional-locks','rev-parse','--verify','HEAD:'+name],cwd=R,capture_output=True)
 assert previous.returncode in [0,128]
 old=previous.stdout.strip().decode() if previous.returncode==0 else None
 if old!=clean:changed_metadata.append(name)
 metadata_git.append(dict(Path=name,GitCleanObjectId=clean,ParentObjectId=old,StageRequired=old!=clean))
assert len(cfg['MetadataPaths'])==14 and set(changed_metadata)<=set(cfg['MetadataPaths'])
output.mkdir(parents=True)
for q,b in copies.items():q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(b)
for q,b in changes.items():q.write_bytes(b)
(output/'evidence-index.json').write_bytes(E(ordered))
receipt=dict(Status='PASS_ACTUAL_MAIN329_BOUNDED_STACK_ENTRY_FULL_NATIVE_READINESS',Passed=6747,Failed=0,IntegrationResults=11,Registry=509,PreviousRegistry=509,RegistryDelta=0,ErlangPresence=351,SourcePaths=12,CSharpPaths=7,SourceClosurePaths=2397,SourceFreezeSha256=H(fp.read_bytes()),SourceClosureSha256=H(closure.read_bytes()),ActualFullGuardSha256=H(fullp.read_bytes()),ActualNativeGuardSha256=H(nativep.read_bytes()),ActualReadinessSha256=H(ready.read_bytes()),ActualFocusedHostSelectors=10,ActualFocusedSdkCases=8,AllRegistryRowsExactParent=True,NoMfaLimitsScopeAppended=True,All6738PreviousOrderedNamesPreserved=True,GeneralClrStackSafetyClaimed=False,Current329OriginalExecuted=False,DeclaredMetadataPaths=14,ActualChangedMetadataPaths=changed_metadata,ActiveVolume='docs/changelog/0051.md',EvidenceIndexSha256=H((output/'evidence-index.json').read_bytes()),Files=ordered)
(output/'evidence-receipt.json').write_bytes(E(receipt))
for x in cfg['ClosedVolumes']:bound(x)
stage=changed_metadata+[x['Path'] for x in source['Files']]+[x['TargetPath'] for x in ordered]+['docs/validation/part-329/evidence-index.json','docs/validation/part-329/evidence-receipt.json']
(control/'explicit-stage-files.json').write_bytes(E(dict(Files=list(dict.fromkeys(stage)),GitExecuted=False)))
(control/'metadata-manifest.json').write_bytes(E(dict(Files=[snapshot(n) for n in cfg['MetadataPaths']],DeclaredMetadataPaths=14,StageMetadataPaths=changed_metadata,CanonicalGitMetadata=metadata_git)))
print(json.dumps(dict(CopiedProofRows=len(ordered),ActiveVolumeBytes=len(volume),ReceiptSha256=H((output/'evidence-receipt.json').read_bytes()))))
