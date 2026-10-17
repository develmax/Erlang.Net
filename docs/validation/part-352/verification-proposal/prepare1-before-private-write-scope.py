from pathlib import Path
import json,hashlib,subprocess,copy,re,collections
R=Path.cwd();A=R/'artifacts';O=A/'six-predicate-published351-contract-proposal';OLD=A/'additional-six-predicate351-contract-audit';H='547a2dd266586589a90c7883dac88d09a5a1de20';OTP='ad05823719d77c8faee87348ea39513d4e2f99c5';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());enc=lambda x:(json.dumps(x,indent=2,ensure_ascii=False)+'\n').encode()
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==H
root='docs/validation/complete-six-predicate-audit/';proof={};origins={}
def add(target,b,origin):assert target not in proof;proof[target]=b;origins[target]=origin
def copyproof(p,target,expected=None):
 b=p.read_bytes()
 if expected:assert sha(b)==expected,(p,sha(b))
 add(root+target,b,p.relative_to(R).as_posix());return dict(Path='validation/complete-six-predicate-audit/'+target,Sha256=sha(b))
def gitblob(p,head=H):return subprocess.check_output(['git','show',head+':'+p])
old=read(OLD/'report.json');assert sha((OLD/'report.json').read_bytes())=='80763329F5A474DEF9687ED654705E7BAD0C1A7063C3206DD074143050370243'
ledgerpath='docs/mfa-compatibility.json';base=read(R/ledgerpath);assert (R/ledgerpath).read_bytes()==gitblob(ledgerpath)
assert len(base['Entries'])==587 and collections.Counter(x['Status'] for x in base['Entries'])=={'Verified compatible':10,'Known differences':24,'Needs verification':553}
guards=base['VerifiedSourceFiles'];assert len(guards)==74
extra=['src/Erlang.Runtime/ErlangBasicRegistrations.cs','src/Erlang.Runtime/ErlangBasicBifNames.cs','src/Erlang.Compiler/AutoImportNames.cs'];union=copy.deepcopy(guards)+[dict(Path=p,Sha256=sha((R/p).read_bytes())) for p in extra];assert len(union)==77
prior={x['Path']:x['Sha256'] for x in old['RuntimeSourceGuardProposal']['ProposedUnion77']};delta=[];snapshots=[]
for row in union:
 path=row['Path'];b=(R/path).read_bytes();assert sha(b)==row['Sha256'] and b==gitblob(path),path
 if prior[path]!=row['Sha256']:delta.append(dict(Path=path,PriorSha256=prior[path],Published351Sha256=row['Sha256']))
 add(root+'source/'+path,b,path);snapshots.append(dict(Path=path,Bytes=len(b),Sha256=sha(b)))
assert len(delta)==1 and delta[0]['Path']=='src/Erlang.Runtime/ModuleRegistry.ProcessScheduling.cs' and delta[0]['Published351Sha256']=='B2032518A4CE88DD09325AFCCDF2E02EB8738F7D1233AC77D8B30DC4A310F17D'
# Independent current branch review anchors, exact expressions and class universe.
texts={r['Path']:(R/r['Path']).read_text() for r in union}
core=texts['src/Erlang.Runtime/CoreModules.cs'];basic=texts[extra[0]];ms=texts['src/Erlang.Runtime/MatchSpecOperations.cs'];scheduling=texts['src/Erlang.Runtime/ModuleRegistry.ProcessScheduling.cs']
for s in ['Boolean(a[0] is Cons or Nil)','Boolean(a[0] is Pid)','ErlangBasicRegistrations.Register(r)','public static Term Boolean(bool value) => Term.A(value ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)']:assert s in core
for s in ['arguments[0] is Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False }','arguments[0] is FunctionTerm','arguments[0] is PortTerm','arguments[0] is ReferenceTerm','ValueTask.FromResult(body(context, arguments))']:assert s in basic
for name in ['is_boolean','is_function','is_list','is_pid','is_port','is_reference']:assert '("'+name+'", 1)' in ms
old_sched=(OLD/'source/src/Erlang.Runtime/ModuleRegistry.ProcessScheduling.cs').read_text();insert='                if (context.Cancellation.IsCancellationRequested && !context.Runtime.IsAlive(context.Self))\n                    throw new ErlangException(ErlangErrorReasons.NoProcess);\n';assert scheduling==old_sched.replace('                context.Runtime.ThrowIfHalted();\n','                context.Runtime.ThrowIfHalted();\n'+insert)
portpath='src/Erlang.Runtime/RuntimePorts.cs';port=(R/portpath).read_bytes();before=gitblob(portpath,'c1f219f31726fc93e537080f400651d300c47d72').decode();after=port.decode();assert after==before.replace('            await previous;','            await previous.ConfigureAwait(false);').replace('            await port.Input.WriteAsync(frame, port.Stop.Token);','            await port.Input.WriteAsync(frame, port.Stop.Token).ConfigureAwait(false);').replace('            await port.Input.FlushAsync(port.Stop.Token);','            await port.Input.FlushAsync(port.Stop.Token).ConfigureAwait(false);')
copyproof(R/portpath,'side-boundary/RuntimePorts.cs')
classes=[]
for p in (R/'src/Erlang.Terms').glob('*.cs'):
 for name in re.findall(r'public (?:sealed )?class (\w+)[^\n]*: Term\b',p.read_text()):classes.append(name)
assert set(classes)==set(old['TermUniverse']['GuestVisibleClasses']) and len(classes)==13
assert 'public sealed class ResourceReferenceTerm : ReferenceTerm' in texts['src/Erlang.Runtime/ResourceReferenceTerm.cs']
anchor_needles={'src/Erlang.Runtime/CoreModules.cs':['Boolean(a[0] is Cons or Nil)','Boolean(a[0] is Pid)','ErlangBasicRegistrations.Register(r)','public static Term Boolean'],extra[0]:['ErlangBasicBifNames.IsBoolean','arguments[0] is FunctionTerm','arguments[0] is PortTerm','arguments[0] is ReferenceTerm'],extra[1]:['(Minimum, 2), (Maximum, 2), (IsBoolean, 1)'],extra[2]:['IsBoolean =','IsFunction =','IsList =','IsPid =','IsPort =','IsReference ='],'src/Erlang.Runtime/ModuleRegistry.cs':['internal Term CallMatchSpecGuard','ValueTask<Term> pending = body(context, arguments);'],'src/Erlang.Compiler/LocalCallValidation.cs':['!SourceCallResolution.IsGuardAutoImport'],'src/Erlang.Compiler/SourceCallResolution.cs':['internal static class SourceCallResolution','public static bool IsGuardAutoImport'],'src/Erlang.Compiler/Execution.AsyncGuards.cs':['Semantics.GuardBifs.Contains'],'src/Erlang.Compiler/CodeGeneration.cs':['Expr.Call c =>','fields.Add(SourceCallBindingCode.Emit'],'src/Erlang.Terms/FunctionTerm.cs':['public sealed class FunctionTerm','public FunctionTerm(ExternalFunctionIdentity','public LocalFunctionIdentity? LocalIdentity']}
anchors=[]
for p,needles in anchor_needles.items():
 lines=texts[p].splitlines();found=[]
 for needle in needles:
  hits=[i for i,l in enumerate(lines) if needle in l];assert hits,(p,needle)
  found.extend(dict(Line=i+1,Needle=needle,Excerpt=lines[max(0,i-1):i+7]) for i in hits)
 anchors.append(dict(Path=p,Sha256=sha((R/p).read_bytes()),Anchors=found))
# Revalidate pinned OTP directly from local immutable git objects, not working-tree sources.
pinned=read(OLD/'pinned-source-anchors.json')
for row in pinned:
 b=subprocess.check_output(['git','-C','../otp','show',OTP+':'+row['Path']]);assert sha(b)==row['Sha256'] and len(b)==row['Bytes']
pinref=copyproof(OLD/'pinned-source-anchors.json','pinned-source-anchors.json')
original=copyproof(A/'part350-ordinary-independent/semantic-guard.json','original350-semantic-guard.json','9A289E0430F92276B7DD691A6269F6E086FC52CBA1BC6EBC2546F1B662A71EC2')
strict=copyproof(A/'part350-ordinary-independent/strict-guard.json','original350-strict-refusal.json','DC15C5EF0448531EA4FADC8D8EFEBFC1DFDC83C99EAA8DA8A82EB1CD868B4E70')
witness=read(OLD/'selected-original350-six-witnesses.json');assert len(witness['Rows'])==6
for row in witness['Rows']:assert row['Passed'] and row['Expected']==row['FixtureExpected']==row['Actual']=='{ok,done}' and sha(row['Source'].encode())==row['SourceSha256']
witnessref=copyproof(OLD/'selected-original350-six-witnesses.json','selected-original350-witnesses.json')
fixturefiles=[]
for n in ['AdditionalTypePredicateCaseNames','AdditionalTypePredicateCaseSources','AdditionalTypePredicateCases']:
 p='tools/Erlang.Differential/'+n+'.cs';b=gitblob(p);assert b==gitblob(p,'c1f219f31726fc93e537080f400651d300c47d72');add(root+'fixture-evidence/'+p,b,p);fixturefiles.append(dict(Path=p,Sha256=sha(b),RuntimeGuard=False))
for p in sorted((OLD/'fixtures').glob('*.erl')):copyproof(p,'fixture-evidence/'+p.name)
full=copyproof(A/'spawn-priority351-full-corrected-independent/report.json','full351-independent.json','B9C1DEFFAB121801E80D278A4512A0CA43787623D8B1C1D0201AF2A5A0FDD165')
ci=copyproof(A/'part351-remote-new-review/ci-independent-guard.json','ci351-independent.json','07D0C11DA131336B24007040710F90A59F8C846161B814B47F0EF28054A0CCA3')
f=read(A/'spawn-priority351-full-corrected-independent/report.json');c=read(A/'part351-remote-new-review/ci-independent-guard.json');assert f['Passed']==c['Passed']==7297 and f['Failed']==c['Failed']==0 and c['Head']==H and c['RuntimeSourceBindings']==74 and c['Restored587CustomLimitsRawExactPrefull']
copyproof(A/'spawn-priority351-port-retired-independent/report.json','two-runtime-source-peer.json')
copyproof(OLD/'report.json','history/initial-prefull351-source-audit.json')
copyproof(OLD/'future-classification-proposal.json','history/initial-unapplied-proposal.json')
# The witnesses' literal source is re-decoded from exact published351 C# constants and compared byte-exactly.
consttext=gitblob('tools/Erlang.Differential/AdditionalTypePredicateCaseSources.cs').decode();literals={n:json.loads(v) for n,v in re.findall(r'public const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;',consttext)};assert len(literals)==6
for row in witness['Rows']:assert row['Source'] in literals.values()
contracts=copy.deepcopy(old['Contracts'])
for x in contracts:
 x['FutureRecommendation']='Verified compatible on complete supported-term observable unary contract; proposal only, requires independent review before application.'
 x['ObservationGapsAreNotUnresolvedSemanticGaps']='The omitted physical/lifecycle witnesses are not falsely claimed as executed. Their result is formally fixed by tag-only branches that read neither arity/code/metadata/liveness nor resource state. Source representation class coverage closes these orthogonal variants, while actual native38x12 rows corroborate each predicate and routing context.'
review=dict(Status='COMPLETE_PUBLISHED351_SIX_UNARY_PREDICATE_CONTRACT_SOURCE_AUDIT',Head=H,PinnedOtp=OTP,PriorAuditSha256=sha((OLD/'report.json').read_bytes()),Contracts=contracts,TermUniverse=old['TermUniverse'],CurrentClosedUniverseIndependentlyRescanned=sorted(classes),SourceBranchAnchors=anchors,PinnedSource=pinref,Current77SourceBindings=union,OnlyChangeFromPrior77=delta,Actual351RuntimeCorrections=dict(Scheduling='Exact added cancelled+absent registry noproc branch before body. Dead HOST context is outside Erlang process invocation model; live guest tag results unchanged. Halt/live cancellation and lease catch remain unchanged.',Ports='Exact three ConfigureAwait(false) private infrastructure Write awaits; no primitive predicate/guard/matchspec branch reads queue state. No port-I/O parity or fullpriority result inferred.'),DispatchArgument=old['DispatchArgument'][:-1]+['Published351 current source/flow, two runtime corrections, exact canonical full7297/11/package2 and current-head CI7297/11 now bound. Prior argument pendingfull state is historical; no primitive handler/guard/import/matchspec semantic branch changed.'],ExhaustivenessArgument=['One valid Erlang term is the complete input domain for each unary MFA. All production guest types partition into13 nominal classes plus the reference-resource subtype. Each tested predicate is a total constant-time tag/name classifier over this finite partition; child contents, list properness, number magnitude, binary alignment, function code/arity and identifier lifetime never enter the branch.','Booleans require exact atom names true/false; all other atoms and every non-Atom class false. AtomCache fallback preserves name and Boolean always returns correct true/false atoms. No truthiness/numeric conversion.','Cons or Nil is the entire list criterion, including improper tails. FunctionTerm covers all local/external/named/captured arities and purged/unavailable code without invoking identityProvider. Sealed Pid/PortTerm classify both local and external representations without liveness. ReferenceTerm pattern includes ResourceReferenceTerm and alias/monitor/magic identities irrespective of lifetime.','NativeRecordTerm is separate from all positive classes. Ordinary record tuples and every remaining negative category are uniformly false. Arbitrary CLR custom subclasses/null/invalid cyclic host constructions are not supported Erlang guest terms; no contract for them is invented.','Normal/apply/named/import and generated C# preserve the chosen MFA delegate. Guards including shadow/no_auto_import and comprehension tests use checked SourceCallResolution+GuardBifs; these are the same six total delegates. Match-spec whitelist/compiler delegates directly to completed body ValueTask, bypassing actor admission; no incomplete-value rejection can occur for these six primitive bodies.','Each valid one-term input returns exactly true or false, with no argument-dependent exception. Wrong arity belongs to a different MFA; errors evaluating an argument or asynchronous process termination do not create an additional unary type-test data branch. No timing/reduction/performance/internal-layout parity claim.'],ActualCorroboration=dict(Original350Semantic=original,Original350StrictRefusal=strict,SelectedOriginalWitnesses=witnessref,Full351=full,CI351=ci),Original351RequiredForTheseSix=False,ReasonOriginal351NotRequired='Published351 fixture constants are raw-identical to actualoriginal350; all six primitive delegate branches/guard/import/matchspec sources remain raw-identical. Sourcegraph correction has no valid live guest unary result branch. Original351 adds three priority scenarios, not these predicates; no wholepriority compatibility is inferred.',NoUnresolvedSemanticGapsFoundForSpecifiedContract=True,ObservedSampleLimitsPreserved=True,CurrentTenVerifiedUnchanged=True,FixtureSourcesEvidenceOnly=fixturefiles,Authorship='Independent of preexisting six predicate production implementations; reviewer authored prior source audit and passive350 reader. Actual OTP captures/full351/CI351 were produced elsewhere and remain separately hash-bound.',MainChanged=False,LedgerApplied=False,SdkExecuted=False,Network=False)
reviewbytes=enc(review);add(root+'complete-source-audit.json',reviewbytes,'new independent current351 complete source/partition/dispatch audit')
# Proposed ledger changes exactly six entries; every old verified and other entry remains identical.
classification=dict(Status='PROPOSED_SIX_COMPLETE_UNARY_CONTRACTS_PUBLISHED351',Head=H,Audit=dict(Path='validation/complete-six-predicate-audit/complete-source-audit.json',Sha256=sha(reviewbytes)),Rows=[dict(Module='erlang',Function=x['Function'],Arity=1,Status='Verified compatible',Scope=x['Scope']+' Complete observable unary semantic contract on the supported Erlang term model; no internal VM layout, timing or performance claim.',Limits=[],Reason='Finite exhaustive type/name branch proof plus actual pinned original38partition/12context corroboration, raw current351 dependency graph and actual351full/CI, not case-only verification.') for x in contracts],Counts=dict(Verified=16,Known=24,NeedsVerification=547),CurrentRuntimeGuards=77,Activated=False,Original351PriorityCapturePending=True,WholePriorityCompatibilityClaimed=False)
classbytes=enc(classification);add(root+'classification.json',classbytes,'prospective exactsix classification based on complete source+actual evidence')
proposed=copy.deepcopy(base);proposed['VerifiedSourceFiles']=union;selectednames={x['Function'] for x in contracts};changed=[]
for b,e in zip(base['Entries'],proposed['Entries']):
 if e['Module']=='erlang' and e['Arity']==1 and e['Function'] in selectednames:
  assert b['Status']=='Needs verification';contract=next(x for x in classification['Rows'] if x['Function']==e['Function']);e['Status']='Verified compatible';e['Scope']=contract['Scope'];e['Limits']=[];e['Evidence'] += [dict(Kind='CompatibilityAudit',Path='validation/complete-six-predicate-audit/classification.json',Sha256=sha(classbytes),Scope='Current published351 complete supported-term unary branch/category/dispatch audit, not selected-case or API-presence inference.'),dict(Kind='OriginalOtpComparison',**witnessref,Scope='Six actual original350 witnesses each38term partitions/12contexts, fixture sources unchanged through published351. Full original pin/report/raw transport guard archived separately.'),dict(Kind='SourceReview',Path='validation/complete-six-predicate-audit/complete-source-audit.json',Sha256=sha(reviewbytes),Scope='Current351 exact77src dependencies, actual full/CI corroboration and unchanged predicate paths, including two runtime correction review. No wholepriority/performance claim.')];changed.append(dict(Module=e['Module'],Function=e['Function'],Arity=1,PriorLimits=b['Limits'],ResolvedBy='Complete source/branch audit plus normal/direct guard/match-spec etc witness coverage.'))
 else:assert b==e
assert len(changed)==6 and collections.Counter(e['Status'] for e in proposed['Entries'])=={'Verified compatible':16,'Known differences':24,'Needs verification':547}
for b,e in zip(base['Entries'],proposed['Entries']):
 if b['Status']=='Verified compatible':assert b==e
assert proposed['VerifiedSourceFiles'][:74]==base['VerifiedSourceFiles']
rows=[]
for target,b in sorted(proof.items()):
 p=O/'proof'/target;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b);rows.append(dict(SourcePath=p.relative_to(R).as_posix(),TargetPath=target,Bytes=len(b),Sha256=sha(b)))
p=O/'ledger-candidate.json';p.write_bytes(enc(proposed))
proposal=dict(Status='ARTIFACT_ONLY_INDEPENDENT_SIX_CONTRACT_CLASSIFICATION_FOR_REVIEW_NOT_APPLIED',ParentHead=H,BaseLedgerSha256=sha((R/ledgerpath).read_bytes()),CandidateLedgerSha256=sha(p.read_bytes()),ExactSixTransitions=changed,Other581EntriesUnchanged=True,AllTenPriorVerifiedRowsExact=True,Prior74SourceBindingPrefixExact=True,RuntimeSourceBindingCount=77,AddedSourcePaths=extra,CountsBefore=[10,24,553],CountsProposed=[16,24,547],ProofFiles=rows,SourceAuditSha256=sha(reviewbytes),ClassificationSha256=sha(classbytes),NoMainOrRuntimeOrSdkOrNetworkMutation=True,FuturePartNumberAssigned=False)
(O/'proposal.json').write_bytes(enc(proposal));(O/'proof-copy-manifest.json').write_bytes(enc(dict(Files=rows)))
print(json.dumps(dict(ProposalSha256=sha((O/'proposal.json').read_bytes()),LedgerSha256=sha(p.read_bytes()),AuditSha256=sha(reviewbytes),ClassificationSha256=sha(classbytes),ProofFiles=len(rows))))
