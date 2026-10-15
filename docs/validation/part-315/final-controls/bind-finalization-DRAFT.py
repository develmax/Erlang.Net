"""DRAFT dynamic config binder. Root must review before execution after actual gates.
Only writes a fresh config directory under artifacts; does not finalize/copy/edit Main.
"""
from pathlib import Path
import argparse,hashlib,json,re
R=Path.cwd().resolve();A=R/'artifacts';P=A/'root-part315-main1';OUT=R/'docs/validation/part-315';HERE=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def read(p):return json.loads(p.read_bytes())
def digest(p):return sha(p.read_bytes())
def write(p,v):p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--native-guard-sha',required=True);parser.add_argument('--readiness-command',required=True);parser.add_argument('--english-entry',required=True);parser.add_argument('--runtime-note',required=True);parser.add_argument('--changelog-volume',default='docs/changelog/0047.md');parser.add_argument('--output',required=True);parser.add_argument('--root-approved-for-finalization',action='store_true');args=parser.parse_args()
 def path(s):
  p=(R/s).resolve();assert p.is_relative_to(R);return p
 bound=path(args.output);assert bound.is_relative_to(A) and not bound.exists()
 native=A/'root-part315-main-windows-native1';ng=native/'guard.json'
 assert digest(P/'full-guard.json')=='E572EC71AED8032A87EF3B4EB36DEFBEDCB22D2C571A23FC26D96444F99FAD27'
 full=read(P/'full-guard.json');assert full['Status']=='PASS_ACTUAL_MAIN315_6642_FULL_AND_ELEVEN_INTEGRATIONS' and (full['Passed'],full['Failed'],full['IntegrationResults'])==(6642,0,11)
 freeze=P/'source-freeze.json';closure=P/'source-resource-closure.json';assert digest(freeze)==full['SourceFreezeSha256']=='C39F8295C15BF566F4491E843391F56519F325A8C7950183415498D4CFAA3B9B'
 assert digest(closure)==full['SourceClosureSha256']=='3883365D8AAC2F8F129A735A8557410E331B3C45DECF556C554A4A93C19ACE1C'
 frozen=read(closure);assert len(frozen['Files'])==2227
 for e in frozen['Files']:
  p=path(e['Path']);assert p.stat().st_size==e['Bytes'] and digest(p)==e['Sha256'],e['Path']
 assert read(P/'full-exit.json')['ExitCode']==0
 assert re.fullmatch('[A-Fa-f0-9]{64}',args.native_guard_sha) and digest(ng)==args.native_guard_sha.upper()
 actual=read(ng);assert actual['Status']=='PASS_ACTUAL_MAIN315_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE'
 assert actual['FullPassed']==6642 and actual['SourceFilesRehashed']==2227 and actual['SourceFreezeSha256']==digest(freeze)
 readiness_command=path(args.readiness_command);assert read(readiness_command)['ExitCode']==0
 ready=R/'docs/module-readiness.json';scope=R/'docs/module-readiness-scope.json';rv=read(ready)
 assert (rv['LocalPassedTests'],rv['RegisteredMfas'],rv['PreviousRegisteredMfas'],rv['RegisteredMfaDelta'],rv['ComparisonTestReport'])==(6642,505,505,0,'validation/part-314/final-main/tests.json')
 assert read(scope)['ComparisonTestReport']==rv['ComparisonTestReport']
 entry=path(args.english_entry);note=path(args.runtime_note);et=entry.read_text(encoding='utf-8-sig');nt=note.read_text(encoding='utf-8-sig').strip()
 assert et.startswith('## Part 315') and '2026-10-16' in et and nt.startswith('Part315:')
 assert '**Final gates are unbound' not in et
 assert not re.search('[\u0400-\u04FF]',et+nt),'English checkpoint text required'
 runtime=[e for e in read(scope)['Components'] if e['Id']=='Runtime'];assert len(runtime)==1 and runtime[0]['Implemented'].count(nt)==1
 rows={}
 def add(source,suffix,expected=None):
  source=source.resolve();assert source.is_relative_to(R) and source.is_file();b=source.read_bytes();assert len(b)<100*1024*1024
  if expected:assert len(b)==expected['Bytes'] and sha(b)==expected['Sha256'],source
  target=OUT/suffix;assert target.is_relative_to(OUT);key=target.relative_to(R).as_posix();assert key not in rows,key
  assert suffix not in ['evidence-index.json','evidence-receipt.json','final-bound-config.json']
  rows[key]={'SourcePath':source.relative_to(R).as_posix(),'Destination':key,'Bytes':len(b),'Sha256':sha(b),'HashMode':'BinaryExact'}
 portable=A/'root-part315-portable1';receipt=read(portable/'archive-replay-receipt.json')
 assert digest(portable/'input-plan.json')=='98EB7B141E63F11D481B74CF34A963B0EBACF92D9AF3D6DFE83CFCC407410FA9'
 assert digest(portable/'archive-reviewed.py')=='C38048D373219E4C1E67D0330692B9DC7C119EE59E853E5D27DDE13B04663366'
 assert digest(portable/'archive-replay-receipt.json')=='2FC71D8032E9AE4FADCFD69816F79DD9ED735AD1D413D1EE9114C1CA079065BB'
 assert receipt['Status']=='PASS_PASSIVE_ARCHIVE_AND_SINGLE_PASS_MEMBER_REHASH' and receipt['ArchiveSha256']=='00F50779462310CE8CC68246D69B684ECE54768A8B2496346C7D6C58E30F0357'
 assert digest(portable/receipt['Archive'])==receipt['ArchiveSha256']
 for n in ['input-plan.json','archive-reviewed.py','prepare-input-plan.py','archive-replay-receipt.json',receipt['Archive'],'remote-separate-copy-index.json','remote-finaldocs-copy-manifest.json']:add(portable/n,'portable/'+n)
 remote=portable/'remote-separate-copy-index.json';assert digest(remote)=='2C0B4BDAF3D7DE89C013E6DB311BAB3AB6C2115FBB7AAF4B24EDFFFD0A943B5E';remote=read(remote)
 assert remote['Head']=='91b0e8ca650937283eb0beb3b3eed0b88bbbe423' and len(remote['RawApiZipRows'])==4 and len(remote['RawJsonLogsAndReviewRows'])==149
 for e in remote['RawApiZipRows']+remote['RawJsonLogsAndReviewRows']:
  p=path(e['SourcePath']);assert p.is_relative_to(A);add(p,'previous-head314/'+p.relative_to(A).as_posix(),e)
 for p in sorted(P.iterdir()):
  if p.is_file():add(p,'final-main/'+p.name)
 add(A/'tests.json','final-main/tests.json');add(A/'integration-results.json','final-main/integration-results.json')
 assert (read(A/'tests.json')['Passed'],read(A/'tests.json')['Failed'])==(6642,0)
 integrations=read(A/'integration-results.json');assert len(integrations)==11
 for role in ['legacy','provider-query','reallocation']:
  for ext in ['attempt.json','process.json','stdout','stderr']:assert (native/(role+'.'+ext)).is_file()
 for p in sorted(native.iterdir()):
  if p.is_file():add(p,'final-native/'+p.name)
 add(readiness_command,'readiness/'+readiness_command.name)
 binaries=A/'root-part315-main-binaries1';br=read(binaries/'receipt.json')
 assert br['Status']=='PASS_PASSIVE_ACTUAL_MAIN315_OUTPUT_ARCHIVE_REHASH' and br['ArchiveSha256']=='6213348943155B0BC9631EDF9254D8F89B1E64C1A857E186F1DA5136594679E3'
 assert digest(binaries/'actual-main315-binaries.zip')==br['ArchiveSha256']
 for n in ['actual-main315-binaries.zip','receipt.json']:add(binaries/n,'final-main/binaries/'+n)
 add(A/'root-archive315-main-binaries.py','final-controls/root-archive315-main-binaries.py')
 binary_peer=A/'root-part315-main-binaries1-beam310-independent-review.json';assert digest(binary_peer)=='3A42551748FCDDFC74866943E4069B2FF97B50B0BB1CF71FD60A0061C3320E62';add(binary_peer,'final-main/binaries/'+binary_peer.name)
 main_peer=A/'root-part315-main-windows-native1-beam310-independent-guard.json';assert digest(main_peer)=='C0CB5EC403457CB1EC90F6A326A34390DA59D8C177F78426EC0D6877EF4EC6D1';add(main_peer,'final-native/'+main_peer.name)
 add(ready,'module-readiness.json')
 for n in ['module-readiness-scope.json','MODULE_READINESS.md','supported-mfas.json']:add(R/'docs'/n,'readiness/'+n)
 # Primary isolated guards/peers copied directly; archived source/output trees stay deduplicated in portable ZIP.
 for rel in ['native-payload-provider315-composed1/actual-focus-independent-guard.json','native-payload-provider315-composed2/formatted-source-freeze.json','native-payload-provider315-composed2/formatted-preservation-proof.json','root-part315-isolated-windows1/guard.json','root-part315-isolated-windows1/manual-typed-review.json','root-part315-isolated-windows1-beam310-independent-guard.json','native-payload-provider315-c-harness-allocator309-source-peer1/review.json','native-payload-provider315-source-peer1/review.json','source-plan-preflight315-beam310-source-peer.json','native-payload-provider315-composed2-beam310-source-peer.json','native-payload-provider315-manual-reader-beam310-source-peer.json']:
  add(A/rel,'isolated-primary/'+rel)
 source_bound=A/'root-part315-finalization-bound1'
 add(source_bound/'finalize315.py','final-controls/finalize315.py');add(source_bound/'config-scope.json','final-controls/config-scope.json')
 for n in ['root-guard315-full.py','root-stage315.py','dispatch315-once.ps1','root315-stage-promotion-guards-beam310-source-peer.json','dispatch315-beam310-source-peer.json']:add(A/n,'final-controls/'+n)
 add(entry,'final-controls/'+entry.name);add(note,'final-controls/'+note.name);add(Path(__file__),'final-controls/'+Path(__file__).name)
 for p in [HERE/'proposal-freeze.json',HERE/'README.md',HERE/'static-copy-manifest.json']:add(p,'final-controls/'+p.name)
 metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/OTP-PORTS.md'];volume=path(args.changelog_volume)
 assert not re.search(rb'## Part 315\b',volume.read_bytes())
 config={'Part':315,'Status':'BOUND_ACTUAL_GATES_FOR_ROOT_FINALIZER_REVIEW_NOT_EXECUTED','ActualReadinessExitCode':0,'SourceFreezeSha256':digest(freeze),'SourceClosureSha256':digest(closure),'FullGuardSha256':digest(P/'full-guard.json'),'NativeGuardSha256':digest(ng),'RuntimeNoteSha256':digest(note),'EnglishEntrySha256':digest(entry),'ChangelogVolume':volume.relative_to(R).as_posix(),'VolumeBeforeSha256':digest(volume),'ChangelogBeforeSha256':digest(R/'CHANGELOG.md'),'MetadataBefore':{n:digest(R/n) for n in metadata},'ReadinessScopeBeforeFinalizerSha256':digest(scope),'RequiredCopiedProofTargets':['final-main/tests.json','final-main/full-guard.json','final-native/guard.json','module-readiness.json'],'ReservedFinalizerGeneratedProofTargets':['evidence-receipt.json'],'EnglishReplacementReviewRequired':True,'Previous314ReallocationFailurePreserved':True,'OriginalNewCounterCasesExecuted':False,'PhysicalMemoryMeasured':False,'Files':[rows[k] for k in sorted(rows)]}
 for e in read(HERE/'static-copy-manifest.json'):
  assert rows[e['Destination']]==e,e['Destination']
 config.update({'RootApprovedForExecution':args.root_approved_for_finalization,'ActualFullGuardSha256':digest(P/'full-guard.json'),'ActualFullStatus':full['Status'],'ActualNativeGuardSha256':digest(ng),'ActualNativeStatus':actual['Status'],'ActualNativeIncludesProviderFivePhases':True,'RuntimeNotePath':note.relative_to(R).as_posix(),'EnglishEntryPath':entry.relative_to(R).as_posix(),'RequiredProofSources':['artifacts/root-part315-isolated-windows1-beam310-independent-guard.json','artifacts/pack314-terminal-independent-summary.json','artifacts/root-part315-main-windows-native1-beam310-independent-guard.json']})
 for source in config['RequiredProofSources']:assert source in {e['SourcePath'] for e in rows.values()},source
 for n in config['RequiredCopiedProofTargets']:assert (OUT/n).relative_to(R).as_posix() in rows,n
 assert not OUT.exists(),'Fresh checkpoint required'
 bound.mkdir();write(bound/'config-finalize.json',config);print(json.dumps({'Config':str(bound/'config-finalize.json'),'Sha256':digest(bound/'config-finalize.json'),'CopyRows':len(rows),'MainModified':False,'FilesCopied':False}))
if __name__=='__main__':main()
