import collections
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
PREFIX = 'validation/part-349/five-predicate-audit/'
PROOF = OUT / 'proof/docs' / PREFIX
HEAD = 'd1bd571fc1f655abf81bdc0336b7133f3d023fac'
NAMES = ['is_atom', 'is_integer', 'is_float', 'is_number', 'is_tuple']

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

copies = []
def exact(source, target, expected=None):
    source = ROOT / source
    if expected:
        assert sha(source) == expected
    destination = PROOF / target
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(source.read_bytes())
    assert sha(destination) == sha(source)
    copies.append({'SourcePath': source.relative_to(ROOT).as_posix(),
                   'TargetPath': 'docs/' + PREFIX + target,
                   'Bytes': source.stat().st_size, 'Sha256': sha(source)})
    return read(source) if source.suffix == '.json' else None

audit = exact('artifacts/type-predicate348-complete-contract-independent/report.json', 'complete-source-review.json', 'EF3B6D92A4C4EEA0E95C405AAAEABB7E4F6A8B5CB5D93F9F5A80ED0DDA87F7AF')
bindings = exact('artifacts/type-predicate348-complete-contract-independent/current-source-bindings.json', 'current-source-bindings.json', '5DB44106023AB0E8506F798696224EE4FF80AC830B427D461F4509246F4188E5')
native = exact('artifacts/part345-ordinary-independent/selected-five-predicate-witnesses.json', 'selected-original-witnesses.json', '9667E452C32EF697CF139208FD3A13C49712DAEC39DE241D84D3EE519B02070A')
exact('artifacts/part345-ordinary-independent/semantic-guard.json', 'original-semantic-guard.json', '334250AA3AC806005A4E867F5BC761FC80753424B9BF498CFCFDCF24ECE0635F')
full = exact('artifacts/term-order348-full-actual2/completion-guard.json', 'full348-producer.json', '202536512FC22F9A72C1CA82CDDDEEEE7607DC25B883276C8AC7E4A8BE1BB346')
exact('artifacts/term-order348-full-independent/report.json', 'full348-independent.json', '4B11CE804C40705B8630D44C84F486399F84C8CF1D91DB22645A963F5FF1055E')
final = exact('artifacts/term-order348-finalization-independent/report.json', 'full348-finalization.json', '80FF7FEF72BB3E4D5E5162C269E9B680121FDD221F34B85BB011463F565068E2')
ci = exact('artifacts/successor348-ci-independent/report.json', 'ci348-independent.json', 'E4B87001BCD9159DC0CD60888C0403AAD59F13209E4A74D5F7719CEFB6D0BD95')
for name in ['run-map.json', 'run.json', 'jobs.json', 'artifacts.json', 'download-manifest.json', 'raw-capture.json', 'job-117236403832.log']:
    exact('artifacts/part348-remote-new-review/ci/' + name, 'ci348/' + name)
for name in ['tests.json', 'integration-results.json']:
    exact('artifacts/part348-remote-new-review/ci/artifact-12080353461/' + name, 'ci348/' + name)
for name in ['reader2-failure.json', 'build-eol-proof.json', 'source-freeze3.json', 'review3.py']:
    exact('artifacts/successor348-ci-independent/' + name, 'ci348/' + name)
exact('artifacts/root-part348-build-eol-review.json', 'ci348/root-build-eol-review.json')

base_path = ROOT / 'docs/mfa-compatibility.json'
base_bytes = base_path.read_bytes()
base = read(base_path)
assert len(base['Entries']) == 587 and len(base['VerifiedSourceFiles']) == 35
assert dict(collections.Counter(e['Status'] for e in base['Entries'])) == {'Verified compatible': 5, 'Known differences': 19, 'Needs verification': 563}
assert ci['Head'] == HEAD and (ci['Passed'], ci['Failed'], ci['Integrations']) == (7268, 0, 11)
assert (ci['Run'], ci['Job'], ci['Artifact']) == (38979387792, 117236403832, 12080353461)
assert ci['All35CurrentSourceBindingsAndAllLedgerEvidenceRehashed'] is True
assert full['Passed'] == final['Passed'] == 7268 and full['Failed'] == final['Failed'] == 0
assert audit['NoConcretePredicateImplementationBlockerFound'] and len(audit['Entries']) == 5
assert bindings['Head'] == HEAD and len(bindings['Files']) == 62
binding_map = {r['Path']: r for r in bindings['Files']}
assert len(binding_map) == 62
for row in bindings['Files']:
    p = ROOT / row['Path']
    assert p.stat().st_size == row['Bytes'] and sha(p) == row['Sha256']
for row in base['VerifiedSourceFiles']:
    assert binding_map[row['Path']]['Sha256'] == row['Sha256']
assert len(native['Rows']) == 5
for row in native['Rows']:
    raw = row['RawRow']
    assert raw['Expected'] == raw['FixtureExpected'] == raw['Actual'] == '{ok,done}'
    assert raw['Passed'] and raw['StandardStreamsMatch']
    assert raw['CallerContext'] == 'UntrappedFresh' and raw['CompilerPolicy'] == 'Default'
    assert hashlib.sha256(raw['Source'].encode('utf-8')).hexdigest().upper() == raw['SourceSha256']
    assert all(raw[k] is None or raw[k] == '' for k in ['NativeStandardOutputHex', 'NativeStandardErrorHex', 'SdkStandardOutputHex', 'SdkStandardErrorHex'])
for row in native['PublishedSourceBindings']:
    assert sha(ROOT / row['Path']) == row['Sha256']

contracts = {
    'is_atom': 'True exactly for Atom terms, including true, false and empty/Unicode atoms.',
    'is_integer': 'True exactly for Integer terms of every admitted BigInteger magnitude.',
    'is_float': 'True exactly for FloatTerm values, including finite signed zero, subnormal and maximum values.',
    'is_number': 'True exactly for Integer or FloatTerm values.',
    'is_tuple': 'True exactly for TupleTerm values of every arity, including ordinary records; distinct NativeRecord values are false.'
}
classification = {
    'Status': 'PASS_COMBINED_EVIDENCE_CLASSIFICATION_PROPOSAL_FIVE_PREDICATES',
    'ParentHead': HEAD, 'PinnedOtpCommit': base['Commit'],
    'OldCounts': {'Verified compatible': 5, 'Known differences': 19, 'Needs verification': 563},
    'ProposedCounts': {'Verified compatible': 10, 'Known differences': 19, 'Needs verification': 558},
    'Entries': [{'Module': 'erlang', 'Function': n, 'Arity': 1, 'Status': 'Verified compatible',
                 'CompleteContract': contracts[n] + ' False for all other admitted guest term classes. The handler only inspects the outer class; deep contents, identity and liveness do not change the result.',
                 'OwnErrorBranches': 'None for a single admitted guest term. Argument-expression failures precede the call; other arities are different MFAs.',
                 'ContextEvidence': 'Current-source audit covers ordinary/qualified calls, apply/external named fun, guards and alternatives, comprehensions/import resolution, and unary table match specifications. Actual original fixture covers 38 term partitions in 12 contexts.',
                 'PhysicalScope': 'Observable supported-term contract; no identical VM layout, scheduling duration, physical resource metric or performance claim.'} for n in NAMES],
    'CurrentSourceBindings': 62, 'ExistingBindingsPreserved': 35, 'AddedBindings': 27,
    'Evidence': copies,
    'CiEolAdmission': 'Reader2 refused 22 inherited project XML/slnx CRLF versus Git LF rows. Reviewed reader3 admits exactly the bound EOL-only variants; actual CI tests/integrations passed. This was a reader precondition failure, not a runtime failure.',
    'FullHistory': 'Full348 completed successfully. Finalization supplement preserves the later children-before metadata overwrite incident and distinguishes reconstructed logical membership from unavailable original serialized bytes; original full outcome and child receipts remain bound.',
    'AllOther582EntriesUnchanged': True, 'OldFiveVerifiedEvidenceUnchanged': True,
    'MainModified': False, 'SdkExecutedByThisProposal': False, 'NetworkUsed': False,
    'StatusPromotionPerformed': False, 'RootReviewRequired': True
}
write(PROOF / 'classification.json', classification)
candidate = copy.deepcopy(base)
old_sources = {r['Path'] for r in base['VerifiedSourceFiles']}
extras = [{'Path': r['Path'], 'Sha256': r['Sha256']} for r in bindings['Files'] if r['Path'] not in old_sources]
assert len(extras) == 27
candidate['VerifiedSourceFiles'].extend(extras)
evidence_targets = [('CompatibilityAudit', 'classification.json'), ('SourceReview', 'complete-source-review.json'),
                    ('SourceReview', 'current-source-bindings.json'), ('OriginalOtpComparison', 'selected-original-witnesses.json'),
                    ('OriginalOtpComparison', 'original-semantic-guard.json'), ('SourceReview', 'full348-producer.json'),
                    ('SourceReview', 'full348-independent.json'), ('SourceReview', 'full348-finalization.json'),
                    ('SourceReview', 'ci348-independent.json')]
changed = []
for old, new in zip(base['Entries'], candidate['Entries']):
    if old['Module'] == 'erlang' and old['Function'] in NAMES and old['Arity'] == 1:
        assert old['Status'] == 'Needs verification'
        new['Status'] = 'Verified compatible'
        new['Scope'] = contracts[old['Function']] + ' False for every other admitted guest term class. Complete observable semantic contract on the supported Erlang term model, with current source branch coverage and actual original 38-partition/12-context witnesses; no identical internal VM layout, physical metrics, timing or performance claim.'
        new['Limits'] = []
        new['Evidence'].extend({'Kind': kind, 'Path': PREFIX + target, 'Sha256': sha(PROOF / target),
                                'Scope': 'Combined per-MFA current348 complete-contract classification; exact original345 witnesses, current348 full/finalization and independently verified published348 CI. See classification.json for precise scope and history.'}
                               for kind, target in evidence_targets)
        assert new['Evidence'][:len(old['Evidence'])] == old['Evidence']
        changed.append(old['Function'])
    else:
        assert new == old
assert sorted(changed) == sorted(NAMES)
assert candidate['VerifiedSourceFiles'][:35] == base['VerifiedSourceFiles']
assert len(candidate['VerifiedSourceFiles']) == 62
counts = dict(collections.Counter(e['Status'] for e in candidate['Entries']))
assert counts == classification['ProposedCounts']
write(OUT / 'ledger-candidate.json', candidate)
write(OUT / 'proof-copy-manifest.json', {'Files': copies, 'FilesCopiedRawExact': len(copies)})
assert base_path.read_bytes() == base_bytes
write(OUT / 'proposal.json', {
    'Status': 'PASS_SOURCE_ONLY_REPORTING349_CLASSIFICATION_PROPOSAL_READY_FOR_ROOT_REVIEW',
    'ParentHead': HEAD, 'BaseLedgerSha256': sha(base_path), 'CandidateLedgerSha256': sha(OUT / 'ledger-candidate.json'),
    'ClassificationSha256': sha(PROOF / 'classification.json'), 'ProofCopyManifestSha256': sha(OUT / 'proof-copy-manifest.json'),
    'ChangedMfas': ['erlang:' + n + '/1' for n in NAMES], 'Entries': 587,
    'Counts': counts, 'AllOther582EntriesUnchanged': True, 'OldFiveVerifiedEvidenceUnchanged': True,
    'Existing35SourceBindingsUnchanged': True, 'AddedSourceBindings': 27, 'CurrentSourceBindings': 62,
    'ActualCiAccepted': True, 'ActualNativeFiveAccepted': True, 'ActualFull348Accepted': True,
    'MainModified': False, 'RuntimeModified': False, 'SdkExecuted': False, 'NetworkUsed': False,
    'RootReviewRequired': True, 'StatusPromotionPerformed': False})
print(json.dumps({'ProposalSha256': sha(OUT / 'proposal.json'), 'CandidateLedgerSha256': sha(OUT / 'ledger-candidate.json'), 'ClassificationSha256': sha(PROOF / 'classification.json'), 'ProofCopies': len(copies), 'Counts': counts}))
