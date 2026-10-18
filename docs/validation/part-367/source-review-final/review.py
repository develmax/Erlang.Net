import copy
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=True) + '\n', encoding='utf-8')

def tokens(text):
    return re.findall(r'"(?:\\.|[^"\\])*"|\S', text)

ledger_path = ROOT / 'docs/mfa-compatibility.json'
ledger = read(ledger_path)
previous = read(ROOT / 'artifacts/queue-integration367-source-review1/proposed-mfa-compatibility.json')
assert ledger == previous, 'Current ledger differs from admitted353 proposal'
guards = ledger['VerifiedSourceFiles']
assert len(guards) == 353
verified = [entry for entry in ledger['Entries'] if entry['Status'] == 'Verified compatible']
assert len(verified) == 38
linear = 'src/Erlang.Runtime/QueueCoreLinear.cs'
changed = [row for row in guards if sha(ROOT / row['Path']) != row['Sha256']]
assert [row['Path'] for row in changed] == [linear], changed
plan_path = ROOT / 'artifacts/queue-core-reverse-successor3/copy-plan.json'
plan = read(plan_path)
assert sha(plan_path) == 'B920CC493E7526FD85CB38BDB857F79DA80F14B61F527DF9C2AF41FA9EE4E854'
payload = []
for row in plan['Files']:
    source, target = ROOT / row['Source'], ROOT / row['Destination']
    assert sha(source) == row['Sha256']
    assert tokens(source.read_text()) == tokens(target.read_text()), row['Destination']
    payload.append({'Path': row['Destination'], 'Bytes': target.stat().st_size, 'Sha256': sha(target), 'FrozenPayloadSha256': sha(source), 'OnlyWhitespaceStyleDelta': True})
new_paths = [
    'src/Erlang.Runtime/QueueCoreReverse.cs',
    'src/Erlang.Runtime/QueueCoreReverseEntry.cs',
    'src/Erlang.Runtime/QueueCoreReverseParameters.cs',
    'src/Erlang.Runtime/ProcessContext.ListTraversal.cs',
]
assert not set(new_paths) & {row['Path'] for row in guards}
add = [{'Path': path, 'Sha256': sha(ROOT / path)} for path in new_paths]
proposal = copy.deepcopy(ledger)
for row in proposal['VerifiedSourceFiles']:
    if row['Path'] == linear:
        row['Sha256'] = sha(ROOT / linear)
proposal['VerifiedSourceFiles'].extend(add)
proposal['VerifiedSourceFiles'].sort(key=lambda row: row['Path'])
assert len(proposal['VerifiedSourceFiles']) == 357
assert proposal['Entries'] == ledger['Entries']
for row in proposal['VerifiedSourceFiles']:
    assert sha(ROOT / row['Path']) == row['Sha256']
write('proposed-mfa-compatibility.json', proposal)
write('guard-delta.json', {
    'BaseLedgerSha256': sha(ledger_path), 'Before': 353, 'After': 357,
    'Refresh': [{'Path': linear, 'PreviousSha256': changed[0]['Sha256'], 'Sha256': sha(ROOT / linear)}],
    'Add': add, 'StatusesChanged': 0, 'All38VerifiedEntriesByteEquivalentJson': True,
    'Other352ExistingSourceGuardsExact': True, 'ApplyOnlyWithExactBaseAndAll357SourceHashes': True,
})
proof = read(ROOT / 'artifacts/queue-core-reverse-successor3/source-proof.json')
write('source-protocol-proof.json', {
    'FrozenSourceProofSha256': sha(ROOT / 'artifacts/queue-core-reverse-successor3/source-proof.json'),
    'PinnedOtp': proof['PinnedOtp'], 'Authority': proof['Authority'],
    'Argument': [
        'Selects native allocation-only reverse branch: managed storage has no BEAM spare-heap model. Forty cells per genuine own remaining reduction; no captured native partition or native words seed execution.',
        'Actor-only RemainingListTraversalReductions reads existing reductions field; ChargeAsync(0) checks controls without consuming reductions. Getter/scheduler source is unchanged.',
        'Each iteration snapshots actual list, accumulator and remaining budget before lookahead. Floor(cells/40) is charged before malformed-lookahead failure, matching native BUMP ordering.',
        'An improper endpoint seen during lookahead raises before any counted cells are copied. Error arguments are the immutable entry pair, not the final scan cursor.',
        'Only after a valid partial chunk is copied does the next iteration snapshot its genuine suffix and accumulated tail. Completing Nil returns reversed values unchanged.',
        'QueueCoreTrace, source locations, effective owner and caller/depth formatting are byte unchanged. Only the reverse BIF effective argument pair can change after a real own trap.',
        'Deterministic actual budget1 tests cover improper prefixes39/40/41/81/80041; enlarged256017 normal/cancel traversal forces cooperative progress at40cells/reduction.',
        'No length cutoff or blanket comparison waiver. Existing160values/800profiles and supplemental11/55 remain actual pending admission gates; any differing error arguments require actual last-entry decomposition and own budget proof.',
    ],
    'SchedulingScope': 'Own managed actor state; no assertion of identical cross-VM trap offsets, native heap availability, allocation timing or native scheduler quantum.',
    'UnaffectedQueueBranches': 'Length, split, append, guards and persistent tuple algorithms unchanged; only out rear-transfer and to_list rear reverse can expose corrected trapped-entry args.',
})
write('source-review.json', {
    'Status': 'FINAL_STYLED_SOURCE_APPLICABILITY_PRESERVES38_QUEUE8_NOT_PROMOTED',
    'BaseLedgerSha256': sha(ledger_path), 'GuardsBefore': 353, 'GuardsAfter': 357,
    'VerifiedEntriesPreserved': 38, 'EntriesChanged': 0, 'NewClassifications': 0,
    'OnlyChangedExistingGuard': linear, 'OtherExistingGuardsExact': 352,
    'FinalPayload': payload, 'AddedDependencies': add,
    'Prior353PreservationProof': {'Path': 'artifacts/queue-integration367-source-review1/source-review.json', 'Sha256': sha(ROOT / 'artifacts/queue-integration367-source-review1/source-review.json')},
    'PriorFullGate': 'Root reports7561/7561 plus11integrations/2packages on source before this reverse delta; not evidence for these final eight styled payloads.',
    'RequiredFinalGates': proof['RequiredGates'],
    'NoMainWritesBuildSdkNativeCiNetwork': True,
})
(OUT / 'README.md').write_text('''# Queue8 final styled source audit\n\nSource-only review preserves all38 existing Verified entries without classification changes. The proposed ledger extends353 guards to357: three new reverse types and the existing actor-budget getter dependency. Only QueueCoreLinear changes among the existing353 guarded sources; the other352 hashes match exactly. All eight final payloads differ from the frozen successor3 only in whitespace.\n\nThe reverse protocol uses genuine own actor state and the pinned allocation-path40cells/reduction rule. It never imports native partition boundaries. Malformed lookahead reports the last immutable BIF-entry pair before copying; later entries use the actual copied suffix and accumulator. Shared scheduler and trace code are unchanged.\n\nNo blanket waiver applies to long-list comparisons. Final recapture160/800, supplemental11/55, deterministic budget boundaries and cooperative cancellation/progress remain admission gates. The earlier passing full7561 run predates this delta. No Queue8 promotion is proposed here. Main was read only; no build, native execution or CI action was performed.\n\nApply guard-delta only when its base ledger hash and every proposed357 source hash match. This folder is immutable after seal.json.\n''', encoding='utf-8')
files = [path for path in OUT.iterdir() if path.is_file() and path.name != 'seal.json']
write('seal.json', {'Files': [{'Path': path.name, 'Bytes': path.stat().st_size, 'Sha256': sha(path)} for path in sorted(files)]})
print(json.dumps({'SealSha256': sha(OUT / 'seal.json'), 'LedgerSha256': sha(OUT / 'proposed-mfa-compatibility.json'), 'DeltaSha256': sha(OUT / 'guard-delta.json'), 'ReviewSha256': sha(OUT / 'source-review.json')}))
