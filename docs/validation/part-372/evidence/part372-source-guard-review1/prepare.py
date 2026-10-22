from pathlib import Path
import json,hashlib,re,collections
O=Path(__file__).resolve().parent;R=O.parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read(p):return json.loads((R/p).read_text(encoding='utf-8-sig'))
def out(n,x):(O/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
def tokens(p):
 pattern=r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_][A-Za-z_0-9]*|[0-9]+|[^\s]'
 return [t for t in re.findall(pattern,p.read_text(encoding='utf-8-sig')) if not t.startswith('//') and not t.startswith('/*')]
ledger=read('docs/mfa-compatibility.json');base=ledger['VerifiedSourceFiles'];assert len(base)==496
changed=[{'Path':x['Path'],'BeforeSha256':x['Sha256'],'AfterSha256':sha(R/x['Path'])} for x in base if sha(R/x['Path'])!=x['Sha256']]
proposals=['lists372-usort-source-proposal1','numeric372-small-parse-proposal1','ordsets372-equality-size-proposal1','ordsets372-filter-continuation-proposal1']
selected={}
for name in proposals:
 for row in read('artifacts/'+name+'/copy-plan.json')['Files']:
  if row['Destination'].startswith('src/'):selected[row['Destination']]=row['Source']
equivalence=[{'Path':p,'ProposalPath':s,'MainSha256':sha(R/p),'ProposalSha256':sha(R/s),'TokenEquivalent':tokens(R/p)==tokens(R/s)} for p,s in sorted(selected.items())]
assert all(x['TokenEquivalent'] for x in equivalence)
assert {x['Path'] for x in changed}<=set(selected)
added=[{'Path':p,'Sha256':sha(R/p)} for p in sorted(selected) if p not in {r['Path'] for r in base}]
guards=[{'Path':r['Path'],'Sha256':sha(R/r['Path'])} for r in base]+added
assert len({r['Path'] for r in guards})==len(guards)
out('verified-source-files.proposed.json',guards)
out('source-proposal-equivalence.json',equivalence)
native=read('docs/native-infrastructure.json');nativechecks=[]
for row in list(native['Evidence'].values())+[s for e in native['Entries'] for s in e['SourceBindings']]:
 nativechecks.append({'Path':row['Path'],'BoundSha256':row['Sha256'],'ActualSha256':sha(R/row['Path'])})
assert all(x['BoundSha256']==x['ActualSha256'] for x in nativechecks)
out('native-source-inventory-review.json',{'Status':'ALL_CURRENT_CLI_SOURCE_AND_EVIDENCE_BINDINGS_MATCH','Checks':nativechecks,'NoInventoryStatusOrHashChangeNeeded':True})
proof={
 'UniqueSourceMachine':'Lists usort public5clause/case tree and34private clauses now share an explicit tail-state machine with the existing84merge clauses. Preserved19oldcurrentMain case bodies token-equivalent proof is in lists372-usort-source-proposal1/existing-merge-preservation.json. Existing reverse/continuation scopes unchanged, new async API used by sole ordinary usort/1 delegate. Parent actualafter1 usort45values225stacks all270strict, eliminating50previous private-helper-frame differences. No complete term-ordering claim.',
 'ListSortAndRegistry':'Only Unique changes from generic LINQ unique Sort to async source machine; ordinary Sort body is byte-text unchanged, sort/1 and key/comparator sort registrations unchanged. Registry sole usort/1 lambda now returns true ValueTask; zf dynamic filtermap alias and callback registrations preserved. Lists public usort is not directly called by current verified queue/proplists/binary kernels; callback code remains actual module dispatch where source requires it.',
 'SmallIntegerParse':'Bridge binary branch unchanged: same ErlangIoData.Binary byte enumeration to ErlangIntegerText.Parse; all arity/base/error capture remains unchanged. Only list_to_integer routes through new source SMALL-to-byte character scanner; two new dependencies bind byte-mask and ordinary improper/sign/radix parsing. list_to_integer currently Needs verification and no promotion is proposed: original numericVM aborted. None of20 existing verified erlang handlers or verified binary/queue/proplists implementations call the integer parse bridge as an internal dependency.',
 'NumericMapEquality':'Only same-count multi-entry flat maps (2..32) in NumericEquality now reuse pinned source key order before exact key alignment and numeric value comparison; nested exact flag is propagated, so exact nested values remain exact. ExactEquality/CanonicalKeyStorage/MapTerm storage/hash routines are unchanged. Current source retains HAMT >32 path (one native6row gap); no whole equality/ordering promotion. Actualafter1 fixes24flatmap logical rows, while source records remaining6HAMT differences.',
 'FilterPcRoles':'Record retains four-argument constructor and adds default Operation role. Every existing non-filter frame has default0, preserving equality/interning among those frames. Only successful filter cons-return pending frame gets named FilterConsReturn. Current error/predicate PC remains distinct from pending return PC despite same source metadata. Repeated cons returns still coalesce; global RuntimeExceptionCapture dedup unchanged. Actual18native differences justify this scope; exact after-fix replay remains required.',
 'ExistingCompleteModules':'binary31 contracts manipulate binary content/options and numeric indexes, not generic unique sort or list_to_integer bridge. queue38 uses its independently ported source algorithms and callback helpers; proplists22 uses exact key/term operations and its own lists-helper/source-execution paths, including raw cursor protocol. Exact equality/storage/hash semantics remain untouched by numeric flatmap alignment. Generic user callbacks retain their own runtime/compiler contracts; opaque module callback wrappers are not broadened into claims about every user callback. Source changes are intentionally narrow; final affected regression is still mandatory, not inferred from source hashes.',
 'ExistingBoundedErlang':'All20 existing complete erlang handlers, their guard allowlists, intrinsic admissions, argument preservation and native failure capture remain unchanged. Shared operator annotation/ModuleRegistry/RuntimeExceptionCapture/ProcessContext sources are byte unchanged in this part. The four newly bounded371 APIs do no payload comparison/parse/sort and do not depend on modified paths.'}
out('source-review.json',{'Status':'INDEPENDENT_SOURCE_GUARD_SUCCESSOR_CONDITIONAL_ON_FINAL_AFFECTED_REGRESSION','BaseLedgerSha256':sha(R/'docs/mfa-compatibility.json'),'ExistingGuardPaths':496,'ExistingGuardUpdates':changed,'UnchangedOldGuardFiles':496-len(changed),'AddedGuardFiles':added,'ProposedGuardFiles':len(guards),'NoOldGuardRemoved':True,'ExistingEntries':len(ledger['Entries']),'ExistingStatuses':dict(collections.Counter(e['Status'] for e in ledger['Entries'])),'NoStatusOrEvidenceChangeProposed':True,'AllSelectedProductionFilesTokenEquivalentToReviewedProposals':True,'SemanticReview':proof,'ExpectedGates':['Exact after-filter correction21-case source packet full-stack replay.','Final affected binary31/queue38/proplists22 and shared deep/snapshot/cancellation regression from parent.','All new HOST groups including reverse failure diagnostic resolved or explicitly remaining; no green-zero-test credit.'],'NoMainBuildOrNetwork':True})
out('freeze.json',{'Files':[{'Path':p.relative_to(O).as_posix(),'Sha256':sha(p)} for p in sorted(O.rglob('*')) if p.is_file() and p.name!='freeze.json'],'ConditionalOnFinalRuntimeGates':True})
print(json.dumps({'Changed':len(changed),'Added':len(added),'FinalGuards':len(guards),'FreezeSha256':sha(O/'freeze.json')}))
