import collections
import copy
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
PREVIOUS = ROOT / 'artifacts/erlang370-verified-source-review1'
PAYLOAD = ROOT / 'artifacts/erlang370-element-continuation-proposal2'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8', newline='\n')


def tokens(path):
    # Passive lexical equivalence only; no source compilation or execution.
    pattern = r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\r\n]*|/\*.*?\*/|\w+|[^\s]'
    return [x for x in re.findall(pattern, path.read_text(encoding='utf-8'), re.S)
            if not x.startswith('//') and not x.startswith('/*')]


base = ROOT / 'docs/mfa-compatibility.json'
ledger = json.loads(base.read_text(encoding='utf-8'))
guards = {r['Path']: r['Sha256'] for r in ledger['VerifiedSourceFiles']}
assert len(guards) == 450 and len(ledger['Entries']) == 678
old_review = json.loads((PREVIOUS / 'review.json').read_text(encoding='utf-8'))
old_rows = {r['Path']: r for r in old_review['ChangedGuards']}
payload = json.loads((PAYLOAD / 'copy-plan.json').read_text(encoding='utf-8'))['Rows']
assert len(payload) == 14
closure = []
for row in payload:
    proposed = PAYLOAD / row['Source']
    current = ROOT / row['Target']
    assert sha(proposed) == row['Sha256']
    assert tokens(proposed) == tokens(current), ('Non-formatting source delta', row['Target'])
    if row['Existing']:
        expected_base = old_rows.get(row['Target'], {}).get('AfterSha256', guards.get(row['Target']))
        assert row['BaseSha256'] == expected_base, ('Base provenance mismatch', row['Target'])
    closure.append({**row, 'CurrentSha256': sha(current), 'TokenEquivalentToReviewedPayload': True})

expected = {
    'src/Erlang.Compiler/CodeGeneration.cs', 'src/Erlang.Compiler/Execution.cs',
    'src/Erlang.Compiler/Expr.cs', 'src/Erlang.Compiler/SourceContinuationBindings.cs',
    'src/Erlang.Compiler/SourceFunctionCode.cs', 'src/Erlang.Runtime/CoreModules.cs',
    'src/Erlang.Runtime/ErlangBasicRegistrations.cs', 'src/Erlang.Runtime/ErlangSizeErrorFrames.cs',
    'src/Erlang.Runtime/ModuleRegistry.cs', 'src/Erlang.Terms/MapTerm.cs',
    'src/Erlang.Terms/Term.cs', 'src/Erlang.Terms/TermOrder.cs'
}
changed = {p for p, h in guards.items() if sha(ROOT / p) != h}
assert changed == expected
assert sha(ROOT / 'src/Erlang.Runtime/ErlangApplyLimits.cs') == guards['src/Erlang.Runtime/ErlangApplyLimits.cs']
additional = {r['Path']: r['Reason'] for r in old_review['AddedGuards']}
additional.update({r['Target']: 'New actual continuation protocol dependency: ' + Path(r['Target']).stem
                   for r in payload if not r['Existing']})
assert len(additional) == 12 and not set(additional) & set(guards)

reasons = {
    'src/Erlang.Compiler/CodeGeneration.cs': 'Generated C# serializes the new call metadata through the explicit Expr.Call constructor. Existing source line/arguments and legacy false defaults retained; no evaluator-only metadata loss.',
    'src/Erlang.Compiler/Execution.cs': 'Previously reviewed bounded dynamic apply block plus reviewed direct/one-hop element successful-return transfer. Actual BIF dispatch executes inside source frame first; only a real successful result with canonical stamp can transfer. Real errors use existing source-frame path. Both named/anonymous frames catch the internal return value; guest try/catch semantics are not used to fabricate a result.',
    'src/Erlang.Compiler/Expr.cs': 'Adds body-continuation metadata and constructor overload; old constructor defaults false. Runtime values, equality and callback argument AST are unchanged.',
    'src/Erlang.Compiler/SourceContinuationBindings.cs': 'Retains existing Apply binding and invokes separate new BIF continuation visitor. The previous algorithm is not replaced with a general optimizer.',
    'src/Erlang.Compiler/SourceFunctionCode.cs': 'Anonymous-function preparation binds the same lexical continuation rules as named module compilation; metadata follows actual source function image.',
    'src/Erlang.Runtime/CoreModules.cs': 'Stamps only genuine canonical element delegate/image after registration; no primitive value, index or badarg behavior rewrite. Host/source/custom installed overrides do not inherit the stamp.',
    'src/Erlang.Runtime/ModuleRegistry.cs': 'Selects canonical stamp atomically under existing gate with actual selected body/image. Avoids racy pre/post dispatch queries; ordinary dispatch-kind behavior remains unchanged.'
}
changed_rows = []
for path in sorted(changed):
    prior = old_rows.get(path)
    if path not in reasons:
        assert prior and sha(ROOT / path) == prior['AfterSha256']
    changed_rows.append({'Path': path, 'Before450Sha256': guards[path], 'CurrentSha256': sha(ROOT / path),
                         'Review': reasons.get(path, prior and prior['Preservation']),
                         'PriorReview': 'artifacts/erlang370-verified-source-review1/review.json' if prior else None})

successor = copy.deepcopy(ledger)
successor['VerifiedSourceFiles'] = [{'Path': p, 'Sha256': sha(ROOT / p)}
                                    for p in sorted(set(guards) | set(additional))]
assert successor['Entries'] == ledger['Entries']
assert len(successor['VerifiedSourceFiles']) == 462
write('mfa-compatibility.proposal.json', successor)
write('review.json', {
    'Status': 'SOURCE_REVIEW_ACCEPTED_FOR_FINAL_CANONICAL_VALIDATION_PENDING_FULL_NO_PROMOTIONS',
    'BaseLedger': {'Path': 'docs/mfa-compatibility.json', 'Sha256': sha(base)},
    'PreservedEntries': 678, 'PreservedStatuses': dict(collections.Counter(r['Status'] for r in ledger['Entries'])),
    'BaselineGuards': 450, 'UnchangedGuards': 438, 'ChangedGuards': changed_rows,
    'AddedGuards': [{'Path': p, 'Sha256': sha(ROOT / p), 'Reason': reason} for p, reason in sorted(additional.items())],
    'ProposedGuardCount': 462,
    'PayloadClosure': {'Path': 'artifacts/erlang370-element-continuation-proposal2/copy-plan.json', 'Sha256': sha(PAYLOAD / 'copy-plan.json'), 'Rows': closure},
    'ApplyLimitRevert': 'Actual current ErlangApplyLimits matches450 baseline exactly; remains internal with1023 unchanged. Prior public-surface amendment is superseded.',
    'SourceProtocolReview': {
        'Scope': 'Conservative literal out-of-domain element and one-hop variable-only local forwarder; no SSA/global optimization claim.',
        'Selection': 'Canonical body/image stamp is captured inside ModuleRegistry gate. CWT ties lifetime to genuine delegate; custom/native override cannot acquire source proof from the signature alone.',
        'Observation': 'AsyncLocal scope matches same ModuleDefinition, ProcessContext, exact analyzed Expr.Call object, argument relationships, actual canonical selection and identical returned result object exactly once; nested predecessor restored and tuple/result references cleared on dispose. Child/late/unrelated events cannot qualify.',
        'Protection': 'Guard/pattern and catch operands blocked; try expression protected, of/catch bodies can inherit only when no after; regions with after declined; comprehensions declined; nested anonymous frames rebound; shared AST allowed/blocked collision blocks transfer.',
        'Failure': 'Actual primitive executes before continuation transfer; real badarg and caller source metadata therefore retained. Internal SourceContinuationReturn is a successful result transport, not a guest exception or a captured expected value.',
        'Metadata': 'Expr.Call annotation survives generated C# constructor, original source line remains, legacy constructor defaults false.',
        'Resources': 'No native resources or code lease owned by observation; existing frame/lease finally paths retain ownership. No additional deep CLR recursive traversal in Terms comparator.'
    },
    'EvidenceBoundary': 'Root reports final build8, all13 permanent continuation controls, old9582/9582 plus new264/264 strict comparisons (9846 total), no relations/differences. This reviewer read source/passive hashes only; canonical full regression remains required and is not claimed complete.',
    'Residuals': ['Known compound-map construction/order gaps remain explicit; no comparison-MFA promotion.', 'No claim all possible OTP SSA optimizations, physical GC/scheduler/reduction/performance identity.', 'No statuses, evidence arrays, limits or existing678 row metadata changed.'],
    'Integration': 'Root may apply this exact462-guard proposal to run final canonical validation now: assert BaseLedger hash, all462 current source hashes, identical Entries. Any later source change requires a new scoped review.'
})
write('freeze.json', {'Status': 'ARTIFACT_ONLY_NO_MAIN_WRITES', 'Files': [
    {'Path': p.relative_to(OUT).as_posix(), 'Sha256': sha(p), 'Bytes': p.stat().st_size}
    for p in sorted(OUT.rglob('*')) if p.is_file() and p.name != 'freeze.json'
]})
print(json.dumps({'ReviewSha256': sha(OUT / 'review.json'), 'LedgerProposalSha256': sha(OUT / 'mfa-compatibility.proposal.json'), 'FreezeSha256': sha(OUT / 'freeze.json'), 'Guards': 462}))
