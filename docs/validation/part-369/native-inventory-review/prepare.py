from pathlib import Path
from collections import Counter
import copy,gzip,hashlib,json
root=Path(__file__).resolve().parents[2];base=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(p,x):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8',newline='\n')
origin=root/'docs/native-infrastructure.json';doc=json.loads(origin.read_bytes());before=copy.deepcopy(doc)
assert len(doc['Entries'])==24 and doc['SourceEquivalence']['CurrentBoundSourceFiles']==40
assert doc['Counts']['InventoryRows']==24 and doc['MfaAccounting']['CountedAsNewMfas']==0
sources={r['Path']:r['Sha256'] for e in doc['Entries'] for r in e['SourceBindings']};assert len(sources)==40
changes=[{'Path':n,'Before':h,'After':sha(root/n)} for n,h in sources.items() if sha(root/n)!=h]
assert {r['Path'] for r in changes}=={'src/Erlang.Runtime/NifNativeBinaryCallbacks.cs','src/Erlang.Runtime/NifNativeBinaryStorage.cs'}
tests=root/'docs/validation/part-369/local-completion/canonical-tests.json';t=json.loads(tests.read_bytes());assert t['Passed']==7639 and t['Failed']==0 and len(t['Tests'])==7639 and all(x['Status']=='Passed' for x in t['Tests'])
assert len({x['Name'] for x in t['Tests']})==7639
phase=root/'docs/validation/part-369/canonical-tests-pass';receipt=json.loads((phase/'canonical-full.exit.json').read_bytes());completion=json.loads((phase/'completion.json').read_bytes())
assert receipt['Settled'] and receipt['ExitCode']==1 and not receipt['TimedOut'] and not receipt['Failures']
stdout=gzip.decompress((phase/'canonical-full.stdout.log.gz').read_bytes());assert hashlib.sha256(stdout).hexdigest().upper()==receipt['StdoutSha256'] and b'7639/7639 passed' in stdout
assert b'Invalid versioned native infrastructure inventory or evidence' in (phase/'canonical-full.stderr.log').read_bytes()
assert sha(phase/'canonical-full.stderr.log')==receipt['StderrSha256'] and completion['SourceChanges']==[] and completion['SourceRows']==3360
guards=root/'docs/validation/part-369/source-guards/guarded-files-proposal.json';g=json.loads(guards.read_bytes())['VerifiedSourceFiles'];assert len(g)==450 and all(sha(root/r['Path'])==r['Sha256'] for r in g)
review=root/'docs/validation/part-369/source-guards/review.json'
plan=root/doc['SourceEquivalence']['TestedPlanPath'];assert sha(plan)==doc['SourceEquivalence']['TestedPlanSha256']
pm={r['Path']:r['Sha256'] for r in json.loads(plan.read_bytes())['Files']}
matches=sum(pm.get(n)==sha(root/n) for n in sources)
for e in doc['Entries']:
 for r in e['SourceBindings']:r['Sha256']=sha(root/r['Path'])
evidence_path='docs/validation/part-369/native-infrastructure-source-addendum.json'
addendum={
 'Status':'DOC_ONLY_CURRENT_UNPUBLISHED_SOURCE_DELTA_NO_NATIVE_SCOPE_PROMOTION',
 'PreviousRegistrySha256':sha(origin),'HistoricalSnapshotMainCommit':before['SnapshotMainCommit'],'CurrentUnpublishedPart':369,
 'HistoricalSnapshotRemainsHistorical':True,'CurrentBindingsAreWorkingTreeNotPublishedCommit':True,
 'RosterRows':24,'BoundSourceFiles':40,'CountsUnchanged':before['Counts'],'SourceChanges':changes,
 'SourceArgument':{
  'NifNativeBinaryCallbacks.cs':'Inspect pins writable bitstrings before binary alignment rejection, as pinned enif_inspect_binary; static subbinary uses genuine logical SliceBits instead of losing parent storage. Existing descriptor/handle/callback ABI unchanged.',
  'NifNativeBinaryStorage.cs':'New FreezeView performs live-pointer/range validation and copies native storage while holding the existing lifetime gate. Large returned views retain a real full-parent managed snapshot; <=64 child copies. No fabricated allocation count; existing physical managed limits remain explicit.'},
 'PinnedOtp':before['PinnedOtp'],
 'HistoricalTestedPlan':{'Path':before['SourceEquivalence']['TestedPlanPath'],'Sha256':sha(plan),'OriginalRegistry40HashesMatchThatPlan':all(pm.get(n)==h for n,h in sources.items()),'Current40ExactMatches':matches,'Current40ExactRawEquivalence':False,'SameHeadExecutionInferred':False},
 'ReviewedCurrentSource':{'Path':review.relative_to(root).as_posix(),'Sha256':sha(review),'GuardsPath':guards.relative_to(root).as_posix(),'GuardsSha256':sha(guards),'Guards':450,'EveryCurrentGuardExact':True,'SourceReviewIsNotIndependentNativeExecution':True},
 'CanonicalTestPhase':{'ReportPath':tests.relative_to(root).as_posix(),'ReportSha256':sha(tests),'Passed':7639,'Failed':0,'OrderedUniqueNames':7639,'ReceiptPath':(phase/'canonical-full.exit.json').relative_to(root).as_posix(),'ReceiptSha256':sha(phase/'canonical-full.exit.json'),'StdoutPath':(phase/'canonical-full.stdout.log.gz').relative_to(root).as_posix(),'StoredStdoutSha256':sha(phase/'canonical-full.stdout.log.gz'),'RawStdoutSha256':receipt['StdoutSha256'],'Pid':receipt['Pid'],'ElapsedSeconds':receipt['ElapsedSeconds'],'ProcessExitCode':1,'SourceRows':3360,'SourceChanges':[],'WholeCanonicalPipelinePassed':False,'LaterFailure':'readiness rejected these stale native-infrastructure source hashes; all7639 test phase checks passed'},
 'ResumePolicy':'Root may apply this docs-only metadata successor and resume the exact readiness/packages suffix against unchanged production/test sources and preserved7639 receipts. No uninterrupted success, repeated test execution, same-head CI or native whole-module verification is inferred.',
 'InventoryScopeUnchanged':True,'HigherVerificationGranted':False,'MfaCountDelta':0,'ImplementationPublished':False,'MainChangedByReviewer':False,
}
dest=base/'source'/evidence_path;save(dest,addendum)
doc['Methodology']+=' SnapshotMainCommit/MainCommit are historical publication/source-presence anchors. The two Part369 working-tree source bindings are explicitly current unpublished deltas; SourceEquivalence links their source/test addendum and does not assert raw equality with the historical Part361 plan.'
doc['Publication']['CurrentUnpublishedSourcePart']=369
doc['Publication']['CurrentSourceBindingsAreWorkingTree']=True
doc['Publication']['HistoricalImplementationCommitUnchanged']=True
doc['SourceEquivalence']['ExactRawHashesMatchTestedPlan']=False
doc['SourceEquivalence']['HistoricalCurrentBoundSourceFiles']=40
doc['SourceEquivalence']['CurrentMatchesHistoricalTestedPlan']=matches
doc['SourceEquivalence']['CurrentUnpublishedDeltaFiles']=changes
doc['SourceEquivalence']['CurrentAddendumPath']=evidence_path
doc['SourceEquivalence']['CurrentAddendumSha256']=sha(dest)
new_evidence={
 'part369-source-delta':{'Path':evidence_path,'Sha256':sha(dest),'Scope':'Docs-only unpublished binary storage/inspect source delta; historical Part361 evidence remains bounded and raw equivalence is false. No higher inventory verification.','Platform':'source-audit','ExecutionIdentity':'part369 current working tree; historical Main372 anchor preserved'},
 'part369-reviewed-sources':{'Path':review.relative_to(root).as_posix(),'Sha256':sha(review),'Scope':'450 reviewed current source guards; source argument, not same-head/native ABI execution.','Platform':'source-only','ExecutionIdentity':'part369 reviewed guard successor'},
 'part369-canonical-tests':{'Path':tests.relative_to(root).as_posix(),'Sha256':sha(tests),'Scope':'Actual7639/0 Windows test phase; whole full2 command later exited1 at readiness inventory validation. Exact suffix resume is separate evidence.','Platform':'windows-x64','ExecutionIdentity':'full2 pid'+str(receipt['Pid'])+'; test phase only'},
}
doc['Evidence'].update(new_evidence)
for e in doc['Entries']:
 if e['Id']=='nif.binary-storage':e['EvidenceRefs']+=list(new_evidence)
assert doc['Counts']==before['Counts'] and doc['InventoryIdsSha256']==before['InventoryIdsSha256']
assert [e['Id'] for e in doc['Entries']]==[e['Id'] for e in before['Entries']]
for old,new in zip(before['Entries'],doc['Entries']):
 for k in old:
  if k not in ['SourceBindings','EvidenceRefs']:assert old[k]==new[k],(old['Id'],k)
save(base/'source/docs/native-infrastructure.json',doc)
save(base/'copy-plan.json',{'Scope':'two docs files only; Root applies and regenerates canonical report using existing tool','Files':[{'Path':'docs/native-infrastructure.json','Source':'source/docs/native-infrastructure.json','BaseSha256':sha(origin),'Sha256':sha(base/'source/docs/native-infrastructure.json')},{'Path':evidence_path,'Source':'source/'+evidence_path,'BaseSha256':None,'Sha256':sha(dest)}]})
save(base/'review.json',{'Status':'PASS_DOC_ONLY_STALE_HASH_SOURCE_REVIEW','Roster':24,'SourceInventory':40,'ChangedBoundSourceFiles':2,'HistoricalTestedPlanCurrentMatches':matches,'OldRosterCountsStatusesCriteriaPreserved':True,'NoVerificationPromotion':True,'NoMfaChange':True,'ToolCodeChangeRequired':False,'Tests7639PassButFull2Exit1Preserved':True,'NoProductionOrTestDelta':True,'ReadyForRootApply':True,'ProposedRegistrySha256':sha(base/'source/docs/native-infrastructure.json'),'AddendumSha256':sha(dest)})
print('docs ready;24/40 preserved; current raw matches historical',matches,'of40')
