from pathlib import Path
import json,hashlib
R=Path.cwd();C=R/'artifacts/guard345-candidate';B=R/'artifacts/length343-candidate';O=R/'artifacts/guard345-verified-source-independent';O.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
frozen=R/'artifacts/guard345-formatted-source/source-freeze.json';assert sha(frozen.read_bytes())=='BE314F56F92CF96F0A28186663C38927A3862AA4937201A97C09A9DF8F9AA58C'
freeze=json.loads(frozen.read_bytes());assert len(freeze['Files'])==13
for row in freeze['Files']:
 raw=(C/row['Path']).read_bytes();assert len(raw)==row['Bytes'] and sha(raw)==row['Sha256']
ledger=json.loads((C/'docs/mfa-compatibility.json').read_bytes());verified=[e for e in ledger['Entries'] if e['Status']=='Verified compatible'];assert {(e['Function'],e['Arity']) for e in verified}=={('self',0),('node',0),('is_map',1),('is_binary',1),('is_bitstring',1)}
oldbindings=ledger['VerifiedSourceFiles'];assert len(oldbindings)==11
unchanged=[]
for row in oldbindings:
 if row['Path']=='src/Erlang.Compiler/Execution.cs':continue
 assert sha((C/row['Path']).read_bytes())==row['Sha256']
 assert (C/row['Path']).read_bytes()==(B/row['Path']).read_bytes();unchanged.append(row['Path'])
old=(B/'src/Erlang.Compiler/Execution.cs').read_text(encoding='utf-8');new=(C/'src/Erlang.Compiler/Execution.cs').read_text(encoding='utf-8')
start=old.index('            case Expr.Call x:\n                {\n                    if (RecordPredicateSyntax.HasLiteralNonpositiveSize(x))');end=old.index('            case ',start+30)
chunk=old[start:end]
chunk=chunk.replace('                    if (RecordPredicateSyntax.HasLiteralNonpositiveSize(x))','                    string? targetModule = x.Module ?? (module?.Functions.Any(f => f.Name == x.Function && f.Arity == x.Arguments.Count) == true ? null : SourceCallResolution.ExternalModule(module, x.Function, x.Arguments.Count));\n                    if (targetModule == ErlangModuleNames.Module && RecordPredicateSyntax.HasLiteralNonpositiveSize(x))')
chunk=chunk.replace('x.Module ?? ErlangModuleNames.Module','targetModule ?? ErlangModuleNames.Module').replace('(x.Module is not null || AutoImportSignatures.Contains(x.Function, args.Length))','targetModule is not null').replace('(x.Module is not null && x.Module != ErlangModuleNames.Module || ErlangSourceWrapperSignatures.Contains(x.Function, args.Length))','(targetModule is not null && targetModule != ErlangModuleNames.Module || targetModule == ErlangModuleNames.Module && ErlangSourceWrapperSignatures.Contains(x.Function, args.Length))')
chunk=chunk.replace('if (RecordPredicateSyntax.TupleTest(x) is { } predicate)','if (targetModule == ErlangModuleNames.Module && RecordPredicateSyntax.TupleTest(x) is { } predicate)').replace('if (x.Module is null && !AutoImportSignatures.Contains(x.Function, args.Length))','if (targetModule is null)')
assert new==old[:start]+chunk+old[end:]
paths={r['Path'] for r in oldbindings}|{r['Path'] for r in freeze['Files']}|{'src/Erlang.Compiler/AutoImportSignatures.cs','src/Erlang.Compiler/RecordPredicateSyntax.cs'}
bindings=[{'Path':p,'Sha256':sha((C/p).read_bytes())} for p in sorted(paths)]
review={'Status':'PASS_SOURCE_ONLY_GUARD345_FIVE_CURRENT_CONTRACT_DEPENDENCY_EXTENSION','ActualCandidate':'artifacts/guard345-candidate','ActualFormattedFreezeSha256':sha(frozen.read_bytes()),'ReviewedOwnedCSharpPaths':13,'UnchangedPriorVerifiedDependencies':unchanged,'ExecutionDeltaExactlyTargetResolutionWithinNormalCallCase':True,'AsyncGuardsAndRestOfExecutionGraphUnchanged343':True,'VerifiedSourceFiles':bindings,'FiveContracts':[],'ExistingClassificationsAndAllOriginalEvidencePreserved':True,'NewMfaCompatibilityPromotion':False,'SdkOrOriginalExecutionClaimed':False,'MainModified':False,'Conditions':['Candidate source must remain exact or this review becomes stale.','The immutable source-image generated initializer carries explicit binding metadata; empty bindings emit no new initializer, preserving ordinary modules.','This review preserves only the five existing bounded valid-Erlang semantic contracts; malformed compile options and compiler warning parity are not promoted.']}
for e in verified:
 function=e['Function'];scope=''
 if function=='self':scope='The calling ProcessContext and direct self handler are unchanged; qualified erlang:self and unsuppressed unqualified self route to the same runtime/module/arity. Local/imported self denotes another MFA, or is rejected in guard by source-guided admission.'
 elif function=='node':scope='Current ProcessRuntime node identity and node/0 registered handler are unchanged; bindings select the erlang node/0 MFA without changing identity snapshots. Local/imported node is a distinct MFA; explicit erlang qualification remains valid.'
 else:scope='Core type classification, BitString length/alignment and MapTerm/NativeRecordTerm boundaries are raw unchanged. Normal dispatch uses targetModule erlang for actual BIF calls; async guard dispatch is raw unchanged after module-aware admission. Local/imported names are classified as ordinary calls in comprehensions, so they cannot silently take the BIF guard fast path.'
 review['FiveContracts'].append({'Module':e['Module'],'Function':function,'Arity':e['Arity'],'Status':e['Status'],'ReviewedContractPreservation':scope,'OriginalEvidenceSha256':sha(json.dumps(e['Evidence'],sort_keys=True,separators=(',',':')).encode())})
target=O/'current-verified-source-review.json';target.write_text(json.dumps(review,indent=2)+'\n',encoding='utf-8')
proposal={'Status':'PROPOSED_METADATA_BINDING_UPDATE_ONLY','VerifiedSourceFiles':bindings,'AppendSourceReviewToFiveVerifiedOnly':{'Kind':'SourceReview','Path':'validation/part-345/current-verified-source-review.json','Sha256':sha(target.read_bytes()),'Scope':'Source-only guard/import/no_auto_import resolution extension for the five existing verified contracts, exact styled345 sources. Existing original comparison evidence and classifications preserved; no additional API promotion.'},'CurrentCompatibilityCounts':{'Verified compatible':5,'Known differences':17,'Needs verification':565},'LedgerNotModified':True}
(O/'binding-update-proposal.json').write_text(json.dumps(proposal,indent=2)+'\n',encoding='utf-8');print(json.dumps({'ReviewSha256':sha(target.read_bytes()),'Bindings':len(bindings),'ProposalSha256':sha((O/'binding-update-proposal.json').read_bytes())}))
