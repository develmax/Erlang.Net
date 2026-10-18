from pathlib import Path
import hashlib,json
R=Path(__file__).resolve().parents[2]; O=Path(__file__).resolve().parent
def read(p): return json.loads((R/p).read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256((R/p).read_bytes()).hexdigest().upper()
def out(n,v): (O/n).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
v=read('docs/native-infrastructure.json')
refs=list(v['Evidence'].values())+[s for e in v['Entries'] for s in e['SourceBindings']]
sources={s['Path']:s['Sha256'] for e in v['Entries'] for s in e['SourceBindings']}
assert len(sources)==v['SourceEquivalence']['CurrentBoundSourceFiles']
checks=[dict(s,ActualSha256=sha(s['Path']),Matches=sha(s['Path'])==s['Sha256']) for s in refs]
assert all(s['Matches'] for s in checks)
out('native-infrastructure-current-review.json',{'Status':'ALL_CLI_VALIDATED_SOURCE_AND_EVIDENCE_HASHES_MATCH','InventorySha256':sha('docs/native-infrastructure.json'),'Validator':'tools/Erlang.Tool/NativeInfrastructureReport.cs','ValidatorSha256':sha('tools/Erlang.Tool/NativeInfrastructureReport.cs'),'SourcePaths':len(sources),'ReferencesIncludingEvidence':len(refs),'Checks':checks,'Conclusion':'No native-infrastructure hash or classification changes are needed. This mirrors the exact CLI Evidence and Entries.SourceBindings validation, including non-src paths; the prior recursive src-only count was narrower.'})
review=read('artifacts/erlang371-final-guard-review1/source-review.json')
review['Status']='INDEPENDENT_CURRENT_MAIN_SOURCE_REVIEW_ACCEPTED_CONDITIONAL_ON_CANONICAL_REGRESSION'
review['PinnedCommit']='ad05823719d77c8faee87348ea39513d4e2f99c5'
review['SemanticDeltaAcceptance']={
 'CompilerFourFiles':'Only operator-token SourceLine propagation and its native error context are added. Unary/Binary public constructors/default 0 remain, guard and short-circuit branches unchanged. Operands execute once before entering operator context. Ordinary Expr.Call path, four bounded BIF dispatches and existing predicate handlers remain unchanged. Matches pinned erl_parse mkop1/mkop2 annotation. Actual final full-stack comparisons close the previously exposed operator-location cases.',
 'CoreModules':'Byte-removing exactly OrdsetCoreModule.Register(r) restores the old guarded hash A538B78EE3F0B0E8A206C69BD0B6A3883F6A8B2F2B310A951C64E4382DBE788A. One new module registration after Queue; no existing handler changed.',
 'ListFunctions':'FoldLeft/Filter/Map/FilterMap alone dispatch to source-faithful kernels extracted into ListCallbackSourceExecution. Ordsets use actual ModuleRegistry lists callbacks including replacement modules; zf preserves tail alias behavior. Remaining ListFunctions code unchanged. Existing verified queue/proplists callbacks use their own source execution kernels; this is not a blanket lists compatibility promotion.',
 'TermOrderAndStep':'Narrow ordinary Ordering/non-exact flatmap key view (2..32 entries) uses pinned OLD_UNIQ key comparison, exact numeric key comparison and ordinary values. Stable iterative auxiliary scopes share comparison budget/context. Exact/NumericEquality/CanonicalKeyStorage and MapTerm/hash storage remain unchanged, preserving distinct NEW_UNIQ keys even when OLD_UNIQ ties. Actual 14 raw map/FUN cases close 84 observed differences; HAMT/whole ordering closure is not claimed.',
 'OrdsetCoreTraceExplicitException':'Only proposed repeated pending-continuation loop becomes one if(pending > 0 && frames.Count < depth). Native add_element uses the same source continuation repeatedly, which OTP records once. Actual malformed recursive errors and full depth packet now match. This sole non-token-equivalent proposal source is explicitly accepted, not a blind hash replacement.',
 'AddedDependencies':'34 added guard paths bind source metadata, new ordsets module, real lists source continuation machine, dispatch helpers, numeric BIF frames and iterative flatmap scope machinery. All 462 existing guard paths remain; 454 are byte unchanged. No existing status or evidence is revoked.'}
review['OriginalComparisons']=[{'Path':p,'Sha256':sha(p),'StrictObservations':n,'Differences':0,'Normalization':False} for p,n in [('artifacts/erlang371-final-comparison1/acceptance.json',4506),('artifacts/ordsets371-fixed-comparison2/acceptance.json',2616)]]
closure=read('artifacts/erlang371-main-final1/capture1/main-dll-closure.json')
checks=[{'Path':x['Path'],'CapturedSha256':x['Sha256'],'LiveSha256':sha(x['Path']),'Matches':sha(x['Path'])==x['Sha256']} for x in closure['Files']]
assert len(checks)==5 and all(x['Matches'] for x in checks)
out('erlang-final-live-dll-closure.json',{'Status':'ALL_FIVE_CAPTURED_DLLS_EQUAL_FINAL_MAIN','OriginalReceiptUnmodified':True,'ResolvesHistoricalFinalRecaptureRequiredFlag':True,'Checks':checks,'MainSource':closure['MainSource'],'PrivateCandidateDllsUsed':closure['PrivateCandidateDllsUsed']})
review['ErlangReceiptFinalClosure']='erlang-final-live-dll-closure.json verifies all five live final Main DLLs against captured hashes; immutable comparison receipt FinalRecaptureRequired flag is historical and not silently edited.'
review['CanonicalExecutionGate']='Parent owns final canonical acceptance; source review does not claim its completion.'
out('source-review.json',review)
print(json.dumps({'GuardProposalSha256':sha('artifacts/erlang371-final-guard-review1/verified-source-files.proposed.json'),'SourceReviewSha256':sha('artifacts/erlang371-final-guard-review1/source-review.json'),'NativeSources':len(sources),'NativeReferences':len(refs)}))
