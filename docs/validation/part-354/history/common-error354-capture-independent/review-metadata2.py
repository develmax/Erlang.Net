import collections
import hashlib
import json
import pathlib
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
proposal = root / 'artifacts/part354-metadata-proposal2'
overlay = proposal / 'overlay'


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


proposal_raw = (proposal / 'proposal.json').read_bytes()
assert sha(proposal_raw) == 'C9C881A919D18477B85DC6478D4B3C6BA2B2302DFF88FFC960C59B23255B8326'
plan_raw = (proposal / 'copy-plan.json').read_bytes()
assert sha(plan_raw) == 'EBD1E596C3791C681322E992506E02D596250CFBE7DD4696C74849DDFFC107DC'
plan = json.loads(plan_raw)
targets = {}
for row in plan['Files']:
    assert row['TargetPath'] not in targets
    path = pathlib.Path(row['SourcePath'])
    assert path.resolve().is_relative_to(proposal.resolve())
    data = path.read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
    if row.get('ParentSha256'):
        assert sha((root / row['TargetPath']).read_bytes()) == row['ParentSha256']
    targets[row['TargetPath']] = row
assert len(targets) == 71
old_raw = (root / 'docs/mfa-compatibility.json').read_bytes()
new_raw = (overlay / 'docs/mfa-compatibility.json').read_bytes()
old = json.loads(old_raw)
new = json.loads(new_raw)
assert new['Tag'] == old['Tag'] == 'OTP-29.1.1'
assert new['Commit'] == old['Commit'] == 'ad05823719d77c8faee87348ea39513d4e2f99c5'
key = lambda row: (row['Module'], row['Function'], row['Arity'])
assert len(old['Entries']) == len(new['Entries']) == 587
assert [key(row) for row in old['Entries']] == [key(row) for row in new['Entries']]
assert len({key(row) for row in new['Entries']}) == 587
expected = {('erlang', 'system_flag', 2), ('erlang', 'load_nif', 2), *(('erlang', 'spawn_request', arity) for arity in (2, 3, 4, 5))}
changed = []
evidence = {}
verified = []
for before, after in zip(old['Entries'], new['Entries']):
    signature = key(before)
    if signature in expected:
        assert before['Status'] == 'Needs verification' and after['Status'] == 'Known differences'
        assert before['Evidence'] == after['Evidence'][:len(before['Evidence'])]
        changed.append(after)
    elif before['Status'] == 'Verified compatible':
        assert {k: v for k, v in before.items() if k != 'Evidence'} == {k: v for k, v in after.items() if k != 'Evidence'}
        assert before['Evidence'] == after['Evidence'][:len(before['Evidence'])]
        assert len(after['Evidence']) > len(before['Evidence'])
        verified.append(signature)
    else:
        assert before == after
    for row in after['Evidence'][len(before['Evidence']):]:
        path = 'docs/' + row['Path']
        assert path in targets
        data = pathlib.Path(targets[path]['SourcePath']).read_bytes()
        assert sha(data) == row['Sha256']
        evidence[path] = row['Sha256']
assert len(changed) == 6 and len(verified) == 16
counts = dict(collections.Counter(row['Status'] for row in new['Entries']))
assert counts == {'Verified compatible': 16, 'Needs verification': 541, 'Known differences': 30}
graph = json.loads((out / 'final-styled3-source-guards.json').read_bytes())['VerifiedSourceFiles']
assert new['VerifiedSourceFiles'] == graph
assert len(graph) == 114
for row in graph:
    view = root / 'artifacts/size-bif354-owned-style-view3' / row['Path']
    data = view.read_bytes() if view.exists() else (root / row['Path']).read_bytes()
    assert sha(data) == row['Sha256']
audit_path = overlay / 'docs/validation/part-354/current-known-differences/source-review.json'
audit_raw = audit_path.read_bytes()
audit = json.loads(audit_raw)
assert audit['PinnedCommit'] == new['Commit']
audits = audit_path.parent
for row in audit['ManagedBindings']:
    data = (root / row['Source']).read_bytes()
    archived = (audits / row['Archived']).read_bytes()
    assert data == archived and len(data) == row['Bytes'] and sha(data) == row['Sha256']
pinned = []
for row in audit['PinnedSources']:
    data = subprocess.check_output(['git', '-C', str(root.parent / 'otp'), 'show', new['Commit'] + ':' + row['Path']])
    excerpt = (audits / row['ExcerptPath']).read_bytes()
    assert sha(data) == row['FullSourceSha256']
    assert sha(excerpt) == row['ExcerptSha256']
    assert excerpt.decode().splitlines() == data.decode().splitlines()[row['FirstLine'] - 1:row['LastLine']]
    pinned.append(row)
report = {
    'Status': 'PASS_INDEPENDENT354_METADATA2_SIX_CURRENT_KNOWN_AND_114_GUARDS_NO_PROMOTION',
    'ProposalSha256': sha(proposal_raw),
    'CopyPlanSha256': sha(plan_raw),
    'CopyPaths': 71,
    'ParentLedgerSha256': sha(old_raw),
    'CandidateLedgerSha256': sha(new_raw),
    'Counts': counts,
    'ChangedMfas': [{'Module': row['Module'], 'Function': row['Function'], 'Arity': row['Arity']} for row in changed],
    'VerifiedScopesLimitsStatusAndEvidencePrefixesPreserved': 16,
    'OtherEntriesExact': 565,
    'All587NamesOrderUniquePreserved': True,
    'CurrentSourceGuards': graph,
    'NewEvidenceBindings': evidence,
    'CurrentKnownAuditSha256': sha(audit_raw),
    'PinnedSourcesVerified': pinned,
    'KnownContractReview': [
        'system_flag2 fullsweep_after0/min_heap_size0: pinned nonnegative-small valid branches versus managed dispatch missing those keys; backtrace_depth implementation is explicitly current and generic error/hd failures historical.',
        'spawn_request2/3/4/5 option-bearing local forms feed shared ProcessSpawnOptions; valid MQD/minheap/minbinheap/fullsweep tuples fall through to badarg then BadOptions. Local valid source options avoid disconnected race. Existing priority/scheduler/connected paths not falsely declared absent.',
        'load_nif2 valid missing library path goes native ddll_open/load_failed versus managed absent backend valid-name fallback constant indirect bad_lib. Current invalid filename42/domain repair expressly acknowledged; no stale invalid42 current counterexample.',
        'UTF8 validator Latin1-launch gap and configured HOST-vs-native ABI distinction remain explicit.',
    ],
    'NoFalseCompletion': 'Proposal has no current-head full/original/CI claim. Earlier private focused34 observations do not promote any API or certify remaining native/private helper/error layouts.',
    'RootFullGateUnchanged': 'Actual final permanent/new reinstall+lambda focus and final composed source/package/plan/closure preflight are separate Root gates.',
    'SdkExecutedByReviewer': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'CompatibilityPromotion': False,
}
target = out / 'metadata2-report.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'Sha256': sha(target.read_bytes())}))
