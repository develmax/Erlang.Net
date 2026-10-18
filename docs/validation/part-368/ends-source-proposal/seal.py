import hashlib
import json
import subprocess
from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'

def digest(value):
    return hashlib.sha256(value).hexdigest().upper()

def write(name, value):
    (OUT/name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')

authority = []
for path in ['lib/stdlib/src/queue.erl', 'lib/stdlib/src/lists.erl']:
    raw = subprocess.check_output(['git', '-C', str(ROOT.parent/'otp'), 'show', PIN+':'+path])
    authority.append({'Path':path,'Bytes':len(raw),'Sha256':digest(raw)})
    target = OUT/'authority'/Path(path).name
    target.parent.mkdir(exist_ok=True)
    target.write_bytes(raw)
exports = [('in_r',2),('out_r',1),('get',1),('get_r',1),('peek',1),('peek_r',1),('drop',1),('drop_r',1),('cons',2),('head',1),('tail',1),('snoc',2),('last',1),('daeh',1),('init',1),('liat',1),('lait',1)]
proofs = {
 'in_r': ['Exact {[],[Only]} redistributes to {[Only],[X]}; other shallow Cons/Nil pairs prepend front without traversing improper tails.', 'Fallback error badarg effective[X,Q], public in_r317.'],
 'out_r': ['Empty returns {empty,Q}; front-only singleton returns removed/new; front-only Cons uses reverse(Tail,[]) then keeps first element in front.', 'Rear singleton and shallow front invokes inlined f2r; rear Cons with non-Nil tail removes head and retains arbitrary tail unvalidated.', 'Fallback badarg[Q] out_r389. Reverse ownerout_r382; inlined f2r length/reverse ownerout_r1310.'],
 'get': ['Shallow pair guard precedes tail get/2. Empty public empty[Q]413; fallback badarg[Q]417.', 'Front Cons returns head ignoring tail; rear-only singleton returns it; rear-only other Cons tail-calls lists:last on rear tail, with no retained get frame.'],
 'get_r': ['Empty empty[Q]446; rear Cons plus shallow front returns head ignoring rear tail; front-only singleton returns it; front-only other Cons tail-calls lists:last(front tail).', 'Fallback badarg[Q]454; no retained get_r continuation for lists:last errors.'],
 'peek': ['Empty atomempty; front Cons and shallow rear returns {value,H}; rear-only singleton returns value; other rear-only Cons wraps lists:last(rear tail).', 'Fallback badarg[Q]482. lists:last failure retains queue:peek/1 at480 because tuple construction follows.'],
 'peek_r': ['Mirror peek with rear priority and front fallback.', 'Fallback badarg[Q]510; wrapping lists:last retains peek_r508.'],
 'drop': ['Empty empty[Q]532; rear-only singleton new; other rear-only Cons reverse tail and remove reversed head while keeping original first rear item.', 'Front singleton plus shallow rear invokes inline r2f; other front Cons returns its tail verbatim, including nonlist.', 'Fallback badarg[Q]543; reverse ownerdrop536; inline r2f ownerdrop1299.'],
 'drop_r': ['Empty empty[Q]565; front-only singleton new; other front-only Cons reverse tail and remove reversed head while keeping original first front item.', 'Rear singleton plus shallow front invokes inline f2r; other rear Cons returns tail verbatim.', 'Fallback badarg[Q]576; reverse ownerdrop_r569; inline f2r ownerdrop_r1310.'],
 'cons': ['Unconditional tail call in_r(X,Q): including publicbadarg[X,Q], ownerin_r, no cons continuation.'],
 'head': ['Own empty[Q]1154 and badarg[Q]1158, shallow pair guard then tailcall get/2; successful selection and lists:last failure same helper chain as get, not public get/1.'],
 'tail': ['Unconditional tail call drop(Q), including empty/badarg ownerdrop and dependency ownerdrop.'],
 'snoc': ['Unconditional tail call in(X,Q) swaps public parameters Q,X. Reuses QueueCoreExecution.In, correct ownerin292 and effective[X,Q], no snoc frame.'],
 'last': ['Unconditional tail call get_r(Q), including empty/badarg ownerget_r and tail lists:last chain.'],
 'daeh': ['Unconditional tail call get_r(Q), no daeh frame.'],
 'init': ['Unconditional tail call drop_r(Q), no init frame.'],
 'liat': ['Unconditional tail call drop_r(Q), no liat frame.'],
 'lait': ['Unconditional tail call drop_r(Q), deprecated name still registered; compiled fixture uses apply to avoid erlc deprecation warning.'],
}
write('branch-proof.json', {
 'Status':'SOURCE_PROPOSAL_NOT_BUILT_OR_CLASSIFIED', 'PinnedOtp':PIN, 'Authority':authority,
 'PerMfa':[{'Module':'queue','Function':name,'Arity':arity,'Clauses':proofs[name],'CompleteRecommendation':'Conditional on source/build/current whole values+raw-stack profiles, cooperative and regression gates; not classified by this source-only proposal.'} for name,arity in exports],
 'Helpers':[
  'lists:last/1 source467 rejects nonCons with function_clause and original single argument; Cons tailcalls last/2. last/2 loops source469; malformed tail fails effective[last element,tail], no error_info. Proper Nil returns last element. Loop is iterative and actor-cooperative.',
  'r2f/f2r0/1/2 preserve exact source representation; larger or improper lists execute length before split and reverse, retaining effective original length input and inline ownerline1299/1310. Never call public from_list because that changes owner metadata.',
  'All reverse calls reuse Part367 actual own reduction-entry snapshot allocation40cells/red protocol. No native reduction partition seeds and no short/long exception waiver.',
  'Shallow source is_list means Cons or Nil, not full proper-list validation. Outputs can intentionally contain nonlist tails; no eager normalization is introduced.',
  'Specialized trace copies established reference-identity continuation compression and actual source locations/depth, skips only current queue source wrapper. Empty and lists:last function_clause cannot use CoreTrace hardcoded badarg.',
 ],
 'Scope':'All supported arbitrary term payloads, shallow tuple/list representations, proper and improper spines; source-defined errors and actual own scheduling controls. No identical native allocation/timing promise.',
 'Pending':'Native fixture owner map_size_finish prepares all30 remaining exports. Last helper source frame/inlining and all alias caller survival must be checked on actual default/0/1/2/8 full raw stacks; no inferred native pass.',
})
dependencies = [
 'src/Erlang.Runtime/QueueModuleNames.cs','src/Erlang.Runtime/QueueCoreModule.cs',
 'src/Erlang.Runtime/QueueCoreExecution.cs','src/Erlang.Runtime/QueueCoreLinear.cs',
 'src/Erlang.Runtime/QueueCoreReverse.cs','src/Erlang.Runtime/QueueCoreTrace.cs',
 'src/Erlang.Runtime/QueueCoreSource.cs','src/Erlang.Runtime/QueueCoreParameters.cs',
 'src/Erlang.Runtime/ProcessContext.ListTraversal.cs','src/Erlang.Runtime/ProcessContext.cs',
 'tests/Erlang.Tests/Program.cs','tests/Erlang.Tests/Erlang.Tests.csproj',
 'tests/Erlang.Tests/RuntimeNodeFoundationHostActor.cs',
 'tools/Erlang.Differential/GeneratedModuleCompiler.cs',
 'tools/Erlang.Differential/CompiledModuleExecution.cs',
]
files = []
for path in sorted((OUT/'source').rglob('*')):
    if path.is_file():
        destination = path.relative_to(OUT/'source').as_posix()
        current = ROOT/destination
        assert not current.exists(), 'New-only destination already exists: '+destination
        files.append({'Source':path.relative_to(ROOT).as_posix(),'Destination':destination,'BaseSha256':None,'Bytes':path.stat().st_size,'Sha256':digest(path.read_bytes())})
write('copy-plan.json', {'Status':'NEW_SOURCE_ONLY_NOT_BUILT', 'Files':files,
 'IntegrationBases':[{'Path':path,'Sha256':digest((ROOT/path).read_bytes())} for path in dependencies],
 'RootOnlyEdits':['Register QueueEndsModule once after existing eight QueueCoreModule registrations.', 'MFA dispatch add QueueEndsMfaContracts.Owns/Run before generic fallback, preserving old ordered names.', 'Register three HOST methods from integration.txt; embed exact new fixture with declared basename. No test replacement.'],
 'NoMainOrCiOrBuildByAuthor':True})
write('test-plan.json', {'TypedMfaChecks':17,'Mfas':[{'Module':'queue','Function':name,'Arity':arity} for name,arity in exports],
 'NewHostNames':['queue-ends-persistent-representation','queue-ends-improper-primary-tail','queue-ends-compiled-all-seventeen'],
 'NewHostCount':3, 'TypedChecks':'One valid representation, invalid argument/error owner per MFA; empty error for applicable unary selectors/removers.',
 'HostCoverage':'Persistence/physical f2r representations0..65, reverse insertion/removal/drop; deliberately improper primary tails retained; lists:last1 error effective argument; compiled all17 aliases over sameQ.',
 'ActualRuns':0,'Builds':0,'ExpectedDefaultNewNamesIfAllTypedDispatched':20,'OriginalWholeStackGates':'Root new30 batch, no promotion from these source-only tests.'})
(OUT/'integration.txt').write_text('''Root-only integration (no shared files edited by author):
QueueEndsModule.Register(registry); // once after existing QueueCoreModule registrations, using actual local registry parameter

if (Erlang.Tests.QueueEndsMfaContracts.Owns(export.Module, export.Function, export.Arity))
{
    await Erlang.Tests.QueueEndsMfaContracts.Run(export.Function, export.Arity);
    return;
}

Test(Erlang.Tests.QueueEndsHostNames.Representation, Erlang.Tests.QueueEndsHostChecks.Representation);
Test(Erlang.Tests.QueueEndsHostNames.Improper, Erlang.Tests.QueueEndsHostChecks.ImproperLists);
Test(Erlang.Tests.QueueEndsHostNames.Compiled, Erlang.Tests.QueueEndsHostChecks.Compiled);

<EmbeddedResource Include="Fixtures/queue_ends368_contract.erl" />
Fixture must compile with sourceFile queue_ends368_contract.erl as in new HOST.
Preserve existing7561 names/order; new17 typed MFA names are produced by normal registry framework plus3 HOST names.
No canonical docs/ledger classification edits included.
''', encoding='utf-8')
(OUT/'README.md').write_text('''# Queue ends and aliases, Part368 isolated proposal

This new-only packet implements17 explicit pinned OTP queue exports: in_r/2, out_r/1, get/1, get_r/1, peek/1, peek_r/1, drop/1, drop_r/1, cons/2, head/1, tail/1, snoc/2, last/1, daeh/1, init/1, liat/1 and lait/1. The other13 remaining exports are owned separately. Existing Part367 implementation and evidence remain unchanged.

Six production classes separate registrations, algorithms, balance helpers, source positions, last-list traversal and error capture. Public MFA strings reuse Root's current QueueModuleNames. Shared CoreLinear/Trace are reused without modifying them. This packet does not call public from_list for balancing or misattribute alias failures to alias names. Head has its own public errors; other true aliases tail-transfer to the pinned target owners. Lists:last errors preserve effective helper arguments, source files, optional peek continuation, actual callers and depth. Proper and improper shallow guards are intentionally distinct from full list validation.

Typed tests cover17 registry MFAs. Three new consolidated HOST names cover representation/persistence, improper tails/helper errors and all17 compiled calls. All accesses use public registry APIs; no internal runtime tests or public diagnostic hooks are introduced. See integration.txt for the exact minimal Root edits and resource registration.

No build, SDK, original VM, CI, Main write, Git mutation or network execution was performed. All native metadata and complete classifications remain conditional on actual whole-value/full-stack comparisons and final gates. Source clauses and pending obligations are explicit in branch-proof.json. Immutable copy-plan destinations are all new; listed current integration/dependency hashes bind the source assumptions and must be re-reviewed if Root's shared-seam edits change them. They are not permission to overwrite those files.
''', encoding='utf-8')
seal_files = [path for path in OUT.rglob('*') if path.is_file() and path.name != 'source-seal.json']
write('source-seal.json', {'Files':[{'Path':path.relative_to(OUT).as_posix(),'Bytes':path.stat().st_size,'Sha256':digest(path.read_bytes())} for path in sorted(seal_files)]})
print(json.dumps({'CopyPlanSha256':digest((OUT/'copy-plan.json').read_bytes()),'ProofSha256':digest((OUT/'branch-proof.json').read_bytes()),'SealSha256':digest((OUT/'source-seal.json').read_bytes()),'PayloadFiles':len(files)}))
